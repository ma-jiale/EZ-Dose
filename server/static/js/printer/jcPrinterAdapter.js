/**
 * JC (精臣) Printer Adapter Implementation
 * 
 * This adapter implements the PrinterAdapter interface for JC brand thermal
 * label printers. It wraps the JC Web SDK to provide a consistent interface
 * that can be used by PrinterManager.
 * 
 * Dependencies:
 *   - jcPrinterSdk_api_third.js (JC SDK API file)
 *   - printerInterface.js (Abstract printer interface)
 * 
 * Supported Features:
 *   - USB and WiFi printer connection
 *   - Label preview and printing
 *   - Print progress monitoring
 *   - Automatic reconnection handling
 * 
 * @author System
 * @version 1.0.0
 */

// ============================================================
// JC Printer Adapter Implementation
// ============================================================

/**
 * JCPrinterAdapter
 * 
 * Implements the PrinterAdapter interface for JC (精臣) label printers.
 * This adapter communicates with the JC print service via WebSocket.
 */
const JCPrinterAdapter = (function () {
    // --------------------------------------------------------
    // Private State
    // --------------------------------------------------------

    // Connection state tracking
    let _serviceConnected = false;
    let _printerConnected = false;
    let _sdkInitialized = false;

    // Current printer information
    let _currentPrinter = null;
    let _printerType = 'NONE'; // 'USB' | 'WIFI' | 'NONE'

    // Cached printer lists
    let _usbPrinters = null;
    let _wifiPrinters = null;

    // Progress listener reference for cleanup
    let _currentProgressListener = null;

    // --------------------------------------------------------
    // Private Helper Functions
    // --------------------------------------------------------

    /**
     * Build the label layout data for a patient label
     * 
     * Creates a complete label configuration including patient name, bed number,
     * and a Code128 barcode for patient identification.
     * 
     * Label Specifications:
     *   - Dimensions: 50mm x 20mm
     *   - Patient Name: Position (2.4, 4.0), Font: 小五 (3.18mm / 9pt)
     *   - Bed Number: Position (2.4, 8.8), Font: 小五 (3.18mm / 9pt)
     *   - Barcode: Position (14.4, 3.5), Size: 32.7mm x 13.3mm, Code128, no text below
     * 
     * Layout Visualization (50mm x 20mm):
     * ┌──────────────────────────────────────────────────┐
     * │ Patient Name       │█████████ BARCODE █████████│
     * │ Bed Number         │███████████████████████████│
     * └──────────────────────────────────────────────────┘
     * 
     * @param {Object} labelData - Patient information for the label
     * @param {string} labelData.patientName - The patient's full name
     * @param {string} labelData.bedNumber - The assigned bed number (e.g., "108床")
     * @param {string} labelData.patientId - Unique patient ID for barcode generation
     * @returns {Object} - Complete label layout configuration for the JC SDK
     */
    function _buildPatientLabelLayout(labelData) {
        const config = LabelConfig;

        // Use 小五 (Xiao Wu) font size - approximately 9pt or 3.18mm
        // This is a standard Chinese typography size suitable for labels
        const fontSize = config.FONT_SIZE ? config.FONT_SIZE.XIAO_WU : 3.18;

        // Build the elements array for the label
        const elements = [
            // Element 1: Patient Name (positioned at top-left of label)
            // This is the primary identifier visible at a glance
            {
                type: 'text',
                json: {
                    x: 2.4,                    // Horizontal position from left edge (mm)
                    y: 4.0,                    // Vertical position from top edge (mm)
                    height: 4.0,               // Text box height - sufficient for single line
                    width: 12.0,               // Text box width - allows for typical Chinese names
                    value: labelData.patientName || '',
                    fontFamily: '宋体',        // Use 宋体 font (matches Demo)
                    rotate: 0,                 // No rotation
                    fontSize: fontSize,        // 小五 size (3.18mm)
                    textAlignHorizonral: config.TEXT_ALIGN.LEFT,   // Left-aligned text
                    textAlignVertical: config.TEXT_VALIGN.CENTER,  // Vertically centered in box
                    letterSpacing: 0.0,        // No extra letter spacing
                    lineSpacing: 1.0,          // Standard line spacing
                    lineMode: 6,               // Default line mode (auto-wrap and fit)
                    fontStyle: [false, false, false, false]  // [Bold, Italic, Underline, Strikethrough]
                }
            },
            // Element 2: Bed Number (positioned below patient name)
            // Format example: "108床" - helps nurses quickly identify patient location
            {
                type: 'text',
                json: {
                    x: 2.4,                    // Same horizontal alignment as name
                    y: 8.8,                    // Positioned below the patient name
                    height: 4.0,               // Same text box height
                    width: 12.0,               // Same width as name field
                    value: labelData.bedNumber ? labelData.bedNumber + '床' : '',
                    fontFamily: '宋体',        // Use 宋体 font (matches Demo)
                    rotate: 0,
                    fontSize: fontSize,
                    textAlignHorizonral: config.TEXT_ALIGN.LEFT,
                    textAlignVertical: config.TEXT_VALIGN.CENTER,
                    letterSpacing: 0.0,
                    lineSpacing: 1.0,
                    lineMode: 6,
                    fontStyle: [false, false, false, false]
                }
            }
        ];

        // Element 3: Patient ID Barcode (positioned on right side of label)
        // Only add barcode if patient ID is provided
        // Uses Code128 format which supports alphanumeric characters
        if (labelData.patientId) {
            elements.push({
                type: 'barCode',
                json: {
                    x: 13.0,                   // Positioned to the right of text elements (moved 1pt left)
                    y: 2.8,                    // Top margin aligned
                    height: 15.0,              // Barcode height in mm (fits within 20mm label)
                    width: 33.0,               // Barcode width in mm (x + width = 48 < 50mm)
                    value: String(labelData.patientId),  // Patient ID as barcode content
                    codeType: config.BARCODE_TYPE ? config.BARCODE_TYPE.CODE128 : 20,  // Code128 format
                    rotate: 0,                 // No rotation
                    fontSize: 3.0,             // Text font size
                    textHeight: 3.0,           // Text height
                    textPosition: config.BARCODE_TEXT_POSITION ? config.BARCODE_TEXT_POSITION.HIDDEN : 2  // Hide text below barcode
                }
            });
        }

        return {
            InitDrawingBoardParam: {
                width: config.width,           // Label width: 50mm
                height: config.height,         // Label height: 20mm
                rotate: config.rotate,         // Canvas rotation angle
                path: config.defaultFontFamily, // Default font file path
                verticalShift: 0,              // Vertical shift (not used)
                HorizontalShift: 0             // Horizontal shift (not used)
            },
            elements: elements
        };
    }

    /**
     * Draw all elements on the label canvas
     * 
     * @param {Array} elements - Array of element definitions to draw
     * @returns {Promise<void>}
     */
    async function _drawElements(elements) {
        if (!elements || elements.length === 0) return;

        for (const item of elements) {
            const json = item.json;
            let res;

            switch (item.type) {
                case 'text':
                    res = await DrawLableText(json);
                    break;
                case 'qrCode':
                    res = await DrawLableQrCode(json);
                    break;
                case 'barCode':
                    res = await DrawLableBarCode(json);
                    break;
                case 'line':
                    res = await DrawLableLine(json);
                    break;
                case 'graph':
                    res = await DrawLableGraph(json);
                    break;
                case 'image':
                    res = await DrawLableImage(json);
                    break;
                default:
                    console.warn('JCPrinterAdapter: Unknown element type:', item.type);
                    continue;
            }

            if (res && res.resultAck && res.resultAck.errorCode !== 0) {
                throw new Error(`Failed to draw ${item.type}: ${res.resultAck.info}`);
            }
        }
    }

    /**
     * Map paper type to SDK code
     * Accepts both string names and numeric codes
     * 
     * @param {string|number} type - Paper type name or code
     * @returns {number} - SDK paper type code
     */
    function _getPaperTypeCode(type) {
        // If already a number, return it directly
        if (typeof type === 'number') {
            return type;
        }

        const typeMap = {
            'gap': 1,        // 间隙纸
            'blackMark': 2,  // 黑标纸
            'continuous': 3, // 连续纸
            'hole': 4,       // 定孔纸
            'transparent': 5,// 透明纸
            'plate': 6,      // 标牌
            'gapBlackMark': 10 // 黑标间隙纸
        };
        return typeMap[type] || 1;
    }

    // --------------------------------------------------------
    // Private SDK Initialization Helper
    // --------------------------------------------------------

    // Flag to prevent concurrent SDK initialization
    let _sdkInitializing = false;

    /**
     * Ensure SDK is initialized before performing operations
     * This is called lazily before preview/print, similar to Demo's manual "初始化SDK" flow
     * 
     * @returns {Promise<boolean>} - True if SDK is ready
     * @private
     */
    async function _ensureSdkInitialized() {
        // Already initialized
        if (_sdkInitialized) {
            return true;
        }

        // Wait if another initialization is in progress
        if (_sdkInitializing) {
            console.log('JCPrinterAdapter: Waiting for SDK initialization in progress...');
            while (_sdkInitializing) {
                await new Promise(r => setTimeout(r, 100));
            }
            return _sdkInitialized;
        }

        // Check if service is connected
        if (!_serviceConnected) {
            console.error('JCPrinterAdapter: Cannot initialize SDK, service not connected');
            return false;
        }

        _sdkInitializing = true;
        console.log('JCPrinterAdapter: Initializing SDK...');

        try {
            const initResult = await initSdk({ fontDir: '' });
            if (initResult.resultAck.errorCode === 0) {
                console.log('JCPrinterAdapter: SDK initialized successfully');
                _sdkInitialized = true;
                return true;
            } else {
                console.error('JCPrinterAdapter: SDK initialization failed:', initResult.resultAck.info);
                return false;
            }
        } catch (error) {
            console.error('JCPrinterAdapter: SDK initialization error:', error);
            return false;
        } finally {
            _sdkInitializing = false;
        }
    }

    return {
        /**
         * Initialize the JC printer SDK and connect to the print service
         * 
         * Similar to Demo's getInstance() call in window.onload:
         * - Only connects WebSocket, does NOT call initSdk() immediately
         * - SDK will be initialized lazily when needed (before preview/print)
         * 
         * @returns {Promise<boolean>} - True if WebSocket connection successful
         */
        initialize: function () {
            return new Promise((resolve, reject) => {
                // Check if SDK functions are available
                if (typeof getInstance !== 'function') {
                    reject(new Error('JC Printer SDK not loaded. Please include jcPrinterSdk_api_third.js'));
                    return;
                }

                // Connect to the print service via WebSocket
                // NOTE: We do NOT call initSdk() here - it will be called lazily
                // This matches Demo's behavior where SDK init is a separate manual step
                getInstance(
                    // onServiceConnected callback
                    () => {
                        console.log('JCPrinterAdapter: Print service connected (SDK will be initialized on first use)');
                        _serviceConnected = true;
                        // Do NOT call initSdk() here - follow Demo's pattern
                        resolve(true);
                    },
                    // onNotSupportedService callback
                    () => {
                        console.error('JCPrinterAdapter: Browser does not support WebSocket');
                        _serviceConnected = false;
                        reject(new Error('Browser does not support print service'));
                    },
                    // onServiceDisconnected callback
                    () => {
                        console.log('JCPrinterAdapter: Print service disconnected');
                        _serviceConnected = false;
                        _printerConnected = false;
                        _sdkInitialized = false;

                        // Notify PrinterManager of disconnection
                        if (typeof PrinterManager !== 'undefined') {
                            PrinterManager.notifyServiceDisconnected();
                        }
                    }
                );
            });
        },

        /**
         * Check if connected to the print service
         * 
         * @returns {boolean}
         */
        isServiceConnected: function () {
            return _serviceConnected;
        },

        /**
         * Check if connected to a printer
         * 
         * @returns {boolean}
         */
        isPrinterConnected: function () {
            return _printerConnected;
        },

        /**
         * Get list of available printers (both USB and WiFi)
         * 
         * @returns {Promise<Array<{name: string, port: number, type: string}>>}
         */
        getAvailablePrinters: async function () {
            const printers = [];

            // Get USB printers
            try {
                const usbResult = await getAllPrinters();
                if (usbResult.resultAck.errorCode === 0) {
                    _usbPrinters = JSON.parse(usbResult.resultAck.info);
                    const usbNames = Object.keys(_usbPrinters);
                    usbNames.forEach(name => {
                        printers.push({
                            name: name,
                            port: parseInt(_usbPrinters[name]),
                            type: 'USB'
                        });
                    });
                }
            } catch (error) {
                console.warn('JCPrinterAdapter: Failed to get USB printers:', error.message);
            }

            // Note: WiFi printer scanning is slow and requires additional setup
            // Uncomment below if WiFi support is needed
            /*
            try {
                const wifiResult = await scanWifiPrinter();
                if (wifiResult.resultAck.errorCode === 0 && wifiResult.resultAck.info) {
                    _wifiPrinters = wifiResult.resultAck.info;
                    _wifiPrinters.forEach(item => {
                        printers.push({
                            name: item.printerName,
                            port: item.tcpPort,
                            type: 'WIFI'
                        });
                    });
                }
            } catch (error) {
                console.warn('JCPrinterAdapter: Failed to get WiFi printers:', error.message);
            }
            */

            return printers;
        },

        /**
         * Connect to a specific printer
         * 
         * @param {Object} printer - Printer object with name, port, and type
         * @returns {Promise<boolean>}
         */
        connectPrinter: async function (printer) {
            // Disconnect existing printer first
            if (_printerConnected) {
                await this.disconnectPrinter();
            }

            try {
                let result;
                if (printer.type === 'WIFI') {
                    result = await connectWifiPrinter(printer.name, parseInt(printer.port));
                } else {
                    result = await selectPrinter(printer.name, parseInt(printer.port));
                }

                if (result.resultAck.errorCode === 0) {
                    _printerConnected = true;
                    _currentPrinter = printer;
                    _printerType = printer.type;
                    console.log(`JCPrinterAdapter: Connected to ${printer.type} printer: ${printer.name}`);

                    // Add status listener for hardware events
                    if (typeof addPrinterStatusListener === 'function') {
                        addPrinterStatusListener(this._onPrinterStatus.bind(this));
                    }

                    return true;
                } else {
                    throw new Error(result.resultAck.info || 'Connection failed');
                }
            } catch (error) {
                console.error('JCPrinterAdapter: Failed to connect printer:', error);
                _printerConnected = false;
                _currentPrinter = null;
                _printerType = 'NONE';
                throw error;
            }
        },

        /**
         * Disconnect from the current printer
         * 
         * @returns {Promise<void>}
         */
        disconnectPrinter: async function () {
            try {
                if (typeof closePrinter === 'function') {
                    await closePrinter();
                }
            } catch (error) {
                console.warn('JCPrinterAdapter: Error during disconnect:', error);
            }

            _printerConnected = false;
            _currentPrinter = null;
            _printerType = 'NONE';

            // Remove status listener
            if (typeof removePrinterStatusListener === 'function') {
                removePrinterStatusListener(this._onPrinterStatus);
            }
        },

        /**
         * Print a label with the specified content and quantity
         * 
         * @param {Object} labelData - Label content (e.g., { patientName: 'John' })
         * @param {number} quantity - Number of labels to print
         * @param {Object} options - Print options (density, paperType, printMode)
         * @returns {Promise<boolean>}
         */
        printLabel: async function (labelData, quantity, options = {}) {
            if (!_printerConnected) {
                throw new Error('Printer not connected');
            }

            // Ensure SDK is initialized before printing (lazy initialization like Demo)
            const sdkReady = await _ensureSdkInitialized();
            if (!sdkReady) {
                throw new Error('SDK initialization failed');
            }

            // Merge with default options
            const printOptions = Object.assign({}, LabelConfig.defaultPrintOptions, options);
            const layout = _buildPatientLabelLayout(labelData);

            return new Promise(async (resolve, reject) => {
                let currentPage = 0;
                const totalPages = 1; // Single page for patient label
                const printQuantity = quantity;

                // Progress listener to handle print job flow
                const progressListener = async (msg) => {
                    try {
                        if (msg.apiName === 'commitJob') {
                            if (msg.resultAck.info === 'commitJob ok!') {
                                // Ready to send page data
                                if (currentPage < totalPages) {
                                    // Initialize drawing board
                                    const initRes = await InitDrawingBoard(layout.InitDrawingBoardParam);
                                    if (initRes.resultAck.errorCode !== 0) {
                                        throw new Error(initRes.resultAck.info);
                                    }

                                    // Draw elements
                                    await _drawElements(layout.elements);

                                    // Commit the page
                                    const commitRes = await commitJob(null, JSON.stringify({
                                        printerImageProcessingInfo: { printQuantity: printQuantity }
                                    }));

                                    if (commitRes.resultAck.errorCode !== 0) {
                                        throw new Error(commitRes.resultAck.info);
                                    }

                                    currentPage++;
                                }
                            } else if (msg.resultAck.printCopies !== undefined) {
                                // Check if all copies are printed
                                if (msg.resultAck.printCopies === printQuantity && msg.resultAck.printPages === totalPages) {
                                    // End the job
                                    const endRes = await endJob();
                                    if (endRes.resultAck.errorCode !== 0) {
                                        throw new Error(endRes.resultAck.info);
                                    }

                                    removeJobListener(progressListener);
                                    resolve(true);
                                }
                            }
                        }
                    } catch (error) {
                        console.error('JCPrinterAdapter: Print error:', error);
                        removeJobListener(progressListener);
                        reject(error);
                    }
                };

                try {
                    // Register the progress listener
                    addJobListener(progressListener);
                    _currentProgressListener = progressListener;

                    // Start the print job
                    const startRes = await startJob(
                        printOptions.density,
                        _getPaperTypeCode(printOptions.paperType),
                        printOptions.printMode,
                        totalPages * printQuantity
                    );

                    if (startRes.resultAck.errorCode !== 0) {
                        removeJobListener(progressListener);
                        reject(new Error(startRes.resultAck.info));
                        return;
                    }
                    // After startJob success, the SDK will send a 'commitJob ok!' 
                    // which will trigger our progressListener

                } catch (error) {
                    console.error('JCPrinterAdapter: startJob error:', error);
                    removeJobListener(progressListener);
                    reject(error);
                }
            });
        },

        /**
         * Generate a preview image of the label
         * 
         * @param {Object} labelData - Label content
         * @returns {Promise<string>} - Base64 encoded image data
         */
        previewLabel: async function (labelData) {
            // Ensure SDK is initialized before preview (lazy initialization like Demo)
            const sdkReady = await _ensureSdkInitialized();
            if (!sdkReady) {
                throw new Error('SDK initialization failed');
            }

            const layout = _buildPatientLabelLayout(labelData);

            try {
                // Initialize drawing board
                const initRes = await InitDrawingBoard(layout.InitDrawingBoardParam);
                if (initRes.resultAck.errorCode !== 0) {
                    throw new Error(initRes.resultAck.info);
                }

                // Draw elements
                await _drawElements(layout.elements);

                // Generate preview image (8 = display scale for 200dpi)
                const previewRes = await generateImagePreviewImage(8);
                if (previewRes.resultAck.errorCode !== 0) {
                    throw new Error(previewRes.resultAck.info);
                }

                const imageInfo = JSON.parse(previewRes.resultAck.info);
                return 'data:image/jpeg;base64,' + imageInfo.ImageData;

            } catch (error) {
                console.error('JCPrinterAdapter: Preview error:', error);
                throw error;
            }
        },

        /**
         * Get the adapter name
         * 
         * @returns {string}
         */
        getAdapterName: function () {
            return 'JC Printer Adapter (精臣打印机)';
        },

        /**
         * Internal handler for printer status changes
         * 
         * @param {Object} res - Status response from SDK
         * @private
         */
        _onPrinterStatus: function (res) {
            if (res.resultAck) {
                if (res.resultAck.callback) {
                    const cb = res.resultAck.callback;
                    if (cb.name === 'onCoverStatusChange') {
                        console.log('JCPrinterAdapter: Cover status:', cb.coverStatus === 0 ? 'Closed' : 'Open');
                    } else if (cb.name === 'onPaperStatusChange') {
                        console.log('JCPrinterAdapter: Paper status:', cb.paperStatus === 1 ? 'Out of paper' : 'Paper loaded');
                    }
                } else if (res.resultAck.online === 'offline') {
                    console.log('JCPrinterAdapter: Printer went offline');
                    _printerConnected = false;
                    if (typeof PrinterManager !== 'undefined') {
                        PrinterManager.notifyPrinterDisconnected();
                    }
                }
            }
        }
    };
})();


// ============================================================
// Auto-register with PrinterManager if available
// ============================================================

// Register the JC adapter as the default adapter when the script loads
if (typeof PrinterManager !== 'undefined') {
    PrinterManager.setAdapter(JCPrinterAdapter);
    console.log('JCPrinterAdapter: Registered as default printer adapter');
}


// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { JCPrinterAdapter };
}
