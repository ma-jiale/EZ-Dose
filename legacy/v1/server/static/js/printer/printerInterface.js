/**
 * Abstract Printer Interface Layer
 * 
 * This module provides an abstraction barrier between the application and specific
 * printer vendor implementations. By using this interface, the application can easily
 * switch to different printer vendors without modifying the business logic code.
 * 
 * Usage:
 *   1. Call PrinterManager.initialize() on page load
 *   2. Use PrinterManager.getPrinters() to get available printers
 *   3. Use PrinterManager.connect(printer) to connect to a printer
 *   4. Use PrinterManager.printPatientLabel(patientName, quantity) to print labels
 * 
 * To switch to a different printer vendor:
 *   1. Create a new adapter implementing the PrinterAdapter interface
 *   2. Register it with PrinterManager.setAdapter(newAdapter)
 * 
 * @author System
 * @version 1.0.0
 */

// ============================================================
// PrinterAdapter Interface Definition
// ============================================================

/**
 * PrinterAdapter Interface
 * 
 * All printer vendor implementations must implement this interface.
 * This ensures consistency across different printer vendors and allows
 * for easy swapping of implementations.
 * 
 * @interface PrinterAdapter
 */
const PrinterAdapterInterface = {
    /**
     * Initialize the printer SDK/service connection
     * @returns {Promise<boolean>} - Returns true if initialization successful
     */
    initialize: async function() { throw new Error('Not implemented'); },

    /**
     * Get the current connection status of the print service
     * @returns {boolean} - True if connected to print service
     */
    isServiceConnected: function() { throw new Error('Not implemented'); },

    /**
     * Get the current connection status of the printer
     * @returns {boolean} - True if connected to a printer
     */
    isPrinterConnected: function() { throw new Error('Not implemented'); },

    /**
     * Get list of available printers
     * @returns {Promise<Array<{name: string, port: number, type: string}>>} - Array of available printers
     */
    getAvailablePrinters: async function() { throw new Error('Not implemented'); },

    /**
     * Connect to a specific printer
     * @param {Object} printer - Printer object with name and port
     * @returns {Promise<boolean>} - Returns true if connection successful
     */
    connectPrinter: async function(printer) { throw new Error('Not implemented'); },

    /**
     * Disconnect from the current printer
     * @returns {Promise<void>}
     */
    disconnectPrinter: async function() { throw new Error('Not implemented'); },

    /**
     * Print a patient label with the specified content and quantity
     * @param {Object} labelData - Label content data
     * @param {string} labelData.patientName - The patient's name to print on the label
     * @param {number} quantity - Number of labels to print
     * @param {Object} options - Additional print options (density, paper type, etc.)
     * @returns {Promise<boolean>} - Returns true if print job submitted successfully
     */
    printLabel: async function(labelData, quantity, options) { throw new Error('Not implemented'); },

    /**
     * Preview the label before printing
     * @param {Object} labelData - Label content data
     * @returns {Promise<string>} - Returns base64 encoded preview image
     */
    previewLabel: async function(labelData) { throw new Error('Not implemented'); },

    /**
     * Get the name of this printer adapter (for display purposes)
     * @returns {string} - Adapter name
     */
    getAdapterName: function() { throw new Error('Not implemented'); }
};


// ============================================================
// PrinterManager - Facade for Printer Operations
// ============================================================

/**
 * PrinterManager
 * 
 * This is the main entry point for all printing operations in the application.
 * It provides a simple facade that delegates to the current printer adapter.
 * 
 * The manager handles:
 * - Adapter registration and switching
 * - Status tracking and event callbacks
 * - Error handling and logging
 */
const PrinterManager = (function() {
    // Private state
    let _adapter = null;
    let _initialized = false;
    let _callbacks = {
        onServiceConnected: [],
        onServiceDisconnected: [],
        onPrinterConnected: [],
        onPrinterDisconnected: [],
        onPrintProgress: [],
        onError: []
    };

    /**
     * Trigger all registered callbacks for a specific event
     * @param {string} eventName - Name of the event
     * @param {*} data - Data to pass to callbacks
     */
    function _triggerCallbacks(eventName, data) {
        if (_callbacks[eventName]) {
            _callbacks[eventName].forEach(cb => {
                try {
                    cb(data);
                } catch (e) {
                    console.error(`Error in ${eventName} callback:`, e);
                }
            });
        }
    }

    return {
        /**
         * Set the printer adapter to use
         * @param {Object} adapter - An object implementing PrinterAdapterInterface
         */
        setAdapter: function(adapter) {
            _adapter = adapter;
            console.log(`PrinterManager: Adapter set to ${adapter.getAdapterName()}`);
        },

        /**
         * Get the current adapter
         * @returns {Object|null} - Current adapter or null
         */
        getAdapter: function() {
            return _adapter;
        },

        /**
         * Initialize the printer system
         * @returns {Promise<boolean>} - True if initialization successful
         */
        initialize: async function() {
            if (!_adapter) {
                console.error('PrinterManager: No adapter set. Call setAdapter() first.');
                return false;
            }

            try {
                const result = await _adapter.initialize();
                _initialized = result;
                if (result) {
                    _triggerCallbacks('onServiceConnected', {});
                }
                return result;
            } catch (error) {
                console.error('PrinterManager: Initialization failed:', error);
                _triggerCallbacks('onError', { message: 'Initialization failed', error: error });
                return false;
            }
        },

        /**
         * Check if the printer manager is initialized
         * @returns {boolean}
         */
        isInitialized: function() {
            return _initialized;
        },

        /**
         * Check if connected to print service
         * @returns {boolean}
         */
        isServiceConnected: function() {
            return _adapter ? _adapter.isServiceConnected() : false;
        },

        /**
         * Check if connected to a printer
         * @returns {boolean}
         */
        isPrinterConnected: function() {
            return _adapter ? _adapter.isPrinterConnected() : false;
        },

        /**
         * Get list of available printers
         * @returns {Promise<Array>}
         */
        getAvailablePrinters: async function() {
            if (!_adapter) {
                throw new Error('No adapter set');
            }
            return await _adapter.getAvailablePrinters();
        },

        /**
         * Connect to a specific printer
         * @param {Object} printer - Printer to connect to
         * @returns {Promise<boolean>}
         */
        connectPrinter: async function(printer) {
            if (!_adapter) {
                throw new Error('No adapter set');
            }

            try {
                const result = await _adapter.connectPrinter(printer);
                if (result) {
                    _triggerCallbacks('onPrinterConnected', { printer: printer });
                }
                return result;
            } catch (error) {
                _triggerCallbacks('onError', { message: 'Failed to connect printer', error: error });
                throw error;
            }
        },

        /**
         * Disconnect from current printer
         * @returns {Promise<void>}
         */
        disconnectPrinter: async function() {
            if (!_adapter) {
                throw new Error('No adapter set');
            }

            await _adapter.disconnectPrinter();
            _triggerCallbacks('onPrinterDisconnected', {});
        },

        /**
         * Print patient label - Main entry point for label printing
         * Accepts a patient data object containing all necessary label information
         * 
         * @param {Object|string} patientData - Patient data object or just the name for backwards compatibility
         * @param {string} patientData.patientName - Patient name to display on label
         * @param {string} patientData.bedNumber - Patient bed number (e.g., "108床")
         * @param {string} patientData.patientId - Patient ID for barcode generation
         * @param {number} quantity - Number of labels to print
         * @param {Object} options - Optional print settings
         * @returns {Promise<boolean>}
         */
        printPatientLabel: async function(patientData, quantity, options = {}) {
            if (!_adapter) {
                throw new Error('No adapter set');
            }

            if (!this.isPrinterConnected()) {
                throw new Error('No printer connected');
            }

            // Support both object format and legacy string format for backwards compatibility
            let labelData;
            if (typeof patientData === 'string') {
                // Legacy format: just patient name
                labelData = {
                    patientName: patientData,
                    bedNumber: '',
                    patientId: ''
                };
            } else {
                // New format: complete patient data object
                labelData = {
                    patientName: patientData.patientName || '',
                    bedNumber: patientData.bedNumber || '',
                    patientId: patientData.patientId || ''
                };
            }

            try {
                const result = await _adapter.printLabel(labelData, quantity, options);
                return result;
            } catch (error) {
                _triggerCallbacks('onError', { message: 'Print failed', error: error });
                throw error;
            }
        },

        /**
         * Preview patient label with all patient information
         * 
         * @param {Object|string} patientData - Patient data object or just the name for backwards compatibility
         * @param {string} patientData.patientName - Patient name to display on label
         * @param {string} patientData.bedNumber - Patient bed number
         * @param {string} patientData.patientId - Patient ID for barcode
         * @returns {Promise<string>} - Base64 preview image
         */
        previewPatientLabel: async function(patientData) {
            if (!_adapter) {
                throw new Error('No adapter set');
            }

            // Support both object format and legacy string format for backwards compatibility
            let labelData;
            if (typeof patientData === 'string') {
                // Legacy format: just patient name
                labelData = {
                    patientName: patientData,
                    bedNumber: '',
                    patientId: ''
                };
            } else {
                // New format: complete patient data object
                labelData = {
                    patientName: patientData.patientName || '',
                    bedNumber: patientData.bedNumber || '',
                    patientId: patientData.patientId || ''
                };
            }

            return await _adapter.previewLabel(labelData);
        },

        /**
         * Register a callback for an event
         * @param {string} eventName - Event name
         * @param {Function} callback - Callback function
         */
        on: function(eventName, callback) {
            if (_callbacks[eventName]) {
                _callbacks[eventName].push(callback);
            }
        },

        /**
         * Remove a callback for an event
         * @param {string} eventName - Event name
         * @param {Function} callback - Callback function to remove
         */
        off: function(eventName, callback) {
            if (_callbacks[eventName]) {
                const index = _callbacks[eventName].indexOf(callback);
                if (index > -1) {
                    _callbacks[eventName].splice(index, 1);
                }
            }
        },

        /**
         * Handle service disconnection (called by adapter)
         */
        notifyServiceDisconnected: function() {
            _initialized = false;
            _triggerCallbacks('onServiceDisconnected', {});
        },

        /**
         * Handle printer disconnection (called by adapter)
         */
        notifyPrinterDisconnected: function() {
            _triggerCallbacks('onPrinterDisconnected', {});
        }
    };
})();


// ============================================================
// Label Configuration
// ============================================================

/**
 * LabelConfig
 * 
 * Configuration for patient label layout and dimensions.
 * These values can be adjusted based on the actual label size used.
 */
const LabelConfig = {
    // Label dimensions in mm (can be customized based on actual label size)
    // Note: If labels print in wrong position, try adjusting these values
    // or perform "auto-learn" on the printer after changing label sizes
    width: 50,
    height: 20,
    rotate: 0,

    // Chinese typography standard font sizes (in mm)
    // Reference: Chinese National Standard GB/T 148
    // These sizes are commonly used in Chinese document formatting
    FONT_SIZE: {
        CHU_HAO: 14.82,      // 初号 (42pt)
        XIAO_CHU: 12.70,     // 小初 (36pt)
        YI_HAO: 9.17,        // 一号 (26pt)
        XIAO_YI: 8.47,       // 小一 (24pt)
        ER_HAO: 7.76,        // 二号 (22pt)
        XIAO_ER: 6.35,       // 小二 (18pt)
        SAN_HAO: 5.64,       // 三号 (16pt)
        XIAO_SAN: 5.29,      // 小三 (15pt)
        SI_HAO: 4.94,        // 四号 (14pt)
        XIAO_SI: 4.23,       // 小四 (12pt)
        WU_HAO: 3.70,        // 五号 (10.5pt)
        XIAO_WU: 3.18,       // 小五 (9pt) - Commonly used for labels and forms
        LIU_HAO: 2.65,       // 六号 (7.5pt)
        XIAO_LIU: 2.29,      // 小六 (6.5pt)
        QI_HAO: 1.94,        // 七号 (5.5pt)
        BA_HAO: 1.59         // 八号 (5pt)
    },

    // Barcode configuration constants
    BARCODE_TYPE: {
        CODE128: 20,         // Standard CODE128 - Good for alphanumeric data
        UPC_A: 21,           // UPC-A - 12 digit retail barcode
        UPC_E: 22,           // UPC-E - Compressed 6 digit UPC
        EAN8: 23,            // EAN-8 - 8 digit European Article Number
        EAN13: 24,           // EAN-13 - 13 digit European Article Number
        CODE93: 25,          // CODE93 - Higher density than CODE39
        CODE39: 26,          // CODE39 - Standard industrial barcode
        CODEBAR: 27,         // CODABAR - Used in libraries, blood banks
        ITF25: 28            // Interleaved 2 of 5 - Numeric only, paired digits
    },

    // Barcode text position options
    BARCODE_TEXT_POSITION: {
        BELOW: 0,            // Display text below the barcode
        ABOVE: 1,            // Display text above the barcode
        HIDDEN: 2            // Do not display text (barcode only)
    },

    // Margins in mm
    marginX: 2.0,
    marginY: 2.0,

    // Default font settings
    defaultFontSize: 5.6,
    defaultFontFamily: 'ZT001.ttf',

    // Text alignment options
    TEXT_ALIGN: {
        LEFT: 0,
        CENTER: 1,
        RIGHT: 2
    },
    TEXT_VALIGN: {
        TOP: 0,
        CENTER: 1,
        BOTTOM: 2
    },

    // Default print options
    defaultPrintOptions: {
        density: 3,           // Print density (1-5 for most models)
        paperType: 1,         // 1: Gap paper, 2: Black mark, 3: Continuous
        printMode: 1          // 1: Thermal, 2: Thermal transfer
    }
};


// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { PrinterManager, LabelConfig };
}
