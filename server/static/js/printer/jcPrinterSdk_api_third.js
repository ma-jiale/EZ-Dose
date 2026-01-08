//websocket
var websocket;
/* //响应数据 */
var ackJsonData;
/**消息列表 */
// [修改] 结构变更：存储对象 { type: 'cb'|'promise', func/resolve/reject: ..., timerId: ... }
var MessageList = {};
// [移除] 移除全局计时器，改为每个请求独立管理
// let timeoutTimer;
var timeout_duration = 10000;

/** 监听器集合 - 任务级 (打印进度) */
// [新增] 专门存储打印任务的进度监听器
let jobListeners = new Set();

/** 监听器集合 - 设备级 (硬件状态) */
// [新增] 专门存储打印机状态的监听器 (开盖、缺纸、离线等)
let statusListeners = new Set();


/**通过websocket发送消息 */
function sendMsg(msg, callback) {
  // console.log("timeout_duration", timeout_duration)
  // console.log('sendMsg', msg.apiName);
  // [移除] 旧的直接赋值逻辑
  // MessageList[msg.apiName] = callback;

  var data = JSON.stringify(msg);

  var tryTimes = 10;
  // [新增] 封装发送与超时逻辑，以便在 Callback 和 Promise 模式中复用
  const performSend = (handler) => {
    if (!websocket || websocket.readyState !== WebSocket.OPEN) {
      const err = new Error("打印服务未开启");
      handleError(handler, err);
      delete MessageList[msg.apiName]; // 清理
      return;
    }



    // [修改] 优化超时处理逻辑
    // [新增] 设置独立的超时计时器，并存入 handler
    // 这样即使有其他消息干扰，也不会清除当前请求的计时器
    handler.timerId = setTimeout(function () {
      console.log('回调超时:', msg.apiName);
      const currentHandler = MessageList[msg.apiName];
      if (currentHandler) {
        const err = new Error("打印服务消息接收超时");
        handleError(currentHandler, err);
        delete MessageList[msg.apiName];
      }
    }, timeout_duration);

    // 发送数据
    for (var i = 0; i < tryTimes; i++) {
      websocket.send(data);
      return;
    }

    for (var i = 0; i < tryTimes; i++) {
      websocket.send(data);
      return;
    }
  };

  // [新增] 统一的错误处理函数
  const handleError = (handler, err) => {
    if (handler.type === 'cb' && typeof handler.func === 'function') {
      handler.func(err);
    } else if (handler.type === 'promise') {
      handler.reject(err);
    }
  };

  // [新增] 判断调用模式：如果有 callback 则走旧逻辑，否则返回 Promise
  if (callback && typeof callback === 'function') {
    // 模式 A: 回调模式 (兼容旧代码)
    MessageList[msg.apiName] = {
      type: 'cb',
      func: callback,
      timerId: null // 预留计时器ID槽位
    };
    performSend(MessageList[msg.apiName]);
  } else {
    // 模式 B: Promise 模式 (新特性)
    return new Promise((resolve, reject) => {
      MessageList[msg.apiName] = {
        type: 'promise',
        resolve: resolve,
        reject: reject,
        timerId: null // 预留计时器ID槽位
      };
      performSend(MessageList[msg.apiName]);
    });
  }

}

/**
 * 初始化打印服务，连接打印服务
 *
 * @param {function} onServiceConnected - 当打印服务连接建立时调用的回调函数。
 * @param {function} onNotSupportedService - 当打印服务服务不支持时调用的回调函数。
 * @param {function} onServiceDisconnected - 当打印服务连接断开时调用的回调函数。
 * @return {undefined} 该函数没有返回值。
 * 
 * @description
 * 1. 所有接口的调用前提是先调用该接口进行打印服务连接。
 * 2. 调用成功后会停止初始化打印服务，如果未调用成功，会间隔3秒调用一次，直到成功连接为止。
 * 3. 建议在页面加载时调用该接口，回调成功后依次调用获取打印机、选择打印机、初始化SDK等接口。
 */
function getInstance(onServiceConnected, onNotSupportedService, onServiceDisconnected) {
  //是否已连接
  let isConnected = false;
  //是否已重连
  let isReconnecting = false;
  //重连时间
  let reconnectTimer = null;

  const connect = () => {

    if ('WebSocket' in window) {
      websocket = new WebSocket('ws://127.0.0.1:37989');
      if ('binaryType' in WebSocket.prototype) {
        websocket.binaryType = 'arraybuffer';

      }

      if ('timeout' in WebSocket.prototype) {
        websocket.timeout = 5000;
      }


      websocket.addEventListener('open', (_) => {
        isConnected = true;
        isReconnecting = false;
        console.log('WebSocket connected !');
        clearInterval(reconnectTimer);
        onServiceConnected();
      });


      websocket.addEventListener('error', (event) => {
        isConnected = false;
        ackJsonData = '';
        console.log('WebSocket error !', event);
        if (!isReconnecting) {
          isReconnecting = true;
          onServiceDisconnected();
          reconnect();
        }

      });

      websocket.addEventListener('close', (event) => {
        isConnected = false;
        ackJsonData = '';
        console.log('WebSocket close !', event);
        if (!isReconnecting) {
          isReconnecting = true;
          onServiceDisconnected();
          reconnect();
        }

      });

      websocket.addEventListener('message', (event) => {
        readCallback(event);
      });

    } else {
      onNotSupportedService();
    }


  };

  const reconnect = () => {
    if (!isConnected && isReconnecting) {
      clearInterval(reconnectTimer);
      reconnectTimer = setInterval(connect, 3000);
    }
  };

  connect();
}


/**
 * 从事件对象中读取回调信息，并根据接收到的数据执行各种操作。
 *
 * @param {object} event - 包含回调信息的事件对象。
 * @param {function} onPrinterDisConnect - 打印机断开连接时要执行的回调函数。
 * @return {undefined} 此函数没有返回值。
 */
function readCallback(event) {
  var callBackInfo = event.data;
  // console.log('readCallback', callBackInfo); // [修改] 减少日志干扰
  // [新增] 兼容处理：如果 binaryType 是 arraybuffer，需要转为字符串才能被 JSON.parse
  if (callBackInfo instanceof ArrayBuffer) {
    // 简单的 TextDecoder 转换，防止 parse 失败
    if (window.TextDecoder) {
      callBackInfo = new TextDecoder("utf-8").decode(callBackInfo);
    } else {
      // 极简回退，假设是纯 ASCII
      callBackInfo = String.fromCharCode.apply(null, new Uint8Array(callBackInfo));
    }
  }
  ackJsonData = callBackInfo;


  if (isJSON(ackJsonData)) {
    var arrParse = JSON.parse(ackJsonData);
    // [修复] 之前报错 "apiName is not defined"，是因为这里漏写了变量声明
    var apiName = arrParse.apiName
  
    // ============================================================
    // 1. 处理“请求-响应”逻辑 (Request-Response)
    //    对应 sendMsg 发出的指令，处理 Promise 或 Callback
    // ============================================================

    // [修改] 核心逻辑：处理接口回调 (Callback 或 Promise)
    // 对于commitJob消息，只有当resultAck.info为'commitJobApi Success!'时才处理回调
    if (MessageList[apiName]) {
      // 特殊处理commitJob消息，只有成功时才进入请求-响应逻辑
      if (apiName === 'commitJob' && arrParse.resultAck && arrParse.resultAck.info !== 'commitJobApi Success!') {
        // commitJob消息但不是成功状态，跳过回调处理，但仍会执行下面的广播逻辑
      } else {
        const handler = MessageList[apiName];
        // 1. 立即清除计时器
        if (handler.timerId) {
          clearTimeout(handler.timerId);
        }
        // 2. [关键修复] 在执行回调前，先把当前的 handler 从列表中移除
        // 防止回调函数内部立即发起同名请求时，新请求的 handler 被这里的 delete 误删
        delete MessageList[apiName];
        // 3. 执行回调
        if (handler.type === 'cb' && typeof handler.func === 'function') {
          // 情况 A: 兼容旧模式，直接回调
          handler.func(null, arrParse);
        } else if (handler.type === 'promise') {
          // 情况 B: 新模式，处理 Promise 状态
          const resultAck = arrParse.resultAck;

          // [新增] 约定：errorCode === 0 为成功，其他为失败
          // 注意：如果某些 API 没有 resultAck，这里需要根据实际情况微调
          if (resultAck && resultAck.errorCode === 0) {
            handler.resolve(arrParse);
          } else {
            // [新增] 构造标准 Error 对象，并将原始数据挂载上去方便调试
            const errorMessage = (resultAck && resultAck.info) ? resultAck.info : 'Unknown Error';
            const error = new Error(errorMessage);
            error.data = arrParse;
            handler.reject(error);
          }
        }
      }
    }

    if (arrParse.apiName == 'configurationWifi') {
      timeout_duration = 10000
    }
    // ============================================================
    // 2. 处理“主动上报/广播”逻辑 (Active Reporting / Broadcast)
    //    无论上面是否处理过，这里都要检查是否需要广播给监听器
    // ============================================================
    // console.log('readCallback', callBackInfo)
    //回调分发
    if (arrParse.apiName == 'commitJob') {
      jobListeners.forEach(listener => {
        try { listener(arrParse); } catch (e) { console.error(e); }
      });
    } else if (arrParse.apiName == 'printStatus'||arrParse.apiName == 'getPrinterHighLevelInfo') {
      statusListeners.forEach(listener => {
        try { listener(arrParse); } catch (e) { console.error(e); }
      });
    }

    ackJsonData = '';

  }
}

/**
 * 初始化SDK，在打印服务连接成功后调用此接口。
 * 在调用SDK的绘制接口之前，必须先调用此接口。
 *
 * @param {object} json - 包含必要参数的JSON对象,格式如下：
 *  {
 *   "fontDir": string, //字体文件目录，默认为""，暂不生效
 * }
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 */
function initSdk(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'initSdk',
    parameter: json
  };
  return sendMsg(jsonObj, callbackFunction);
}


/**
 * 获取所有打印机信息。
 *
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 需要在打印服务连接成功后调用此函数，建议在打印服务连接成功的回调函数中调用。
 * 注意：此函数只能获取 USB连接的打印机列表。
 */
function getAllPrinters(callbackFunction = null) {
  //刷新设备时，关闭设备
  //closePrinter();
  var jsonObj = { apiName: 'getAllPrinters' };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 搜索Wifi打印机
 *
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 需要在打印服务连接成功后调用此函数，建议在打印服务连接成功的回调函数中调用。
 */
function scanWifiPrinter(callbackFunction = null) {
  timeout_duration = 25000;
  //刷新设备时，关闭设备
  //closePrinter();
  var jsonObj = { apiName: 'scanWifiPrinter' };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}


/**
 * 发送消息以选择打印机。
 *
 * @param {string} printerName - 打印机名称。
 * @param {number} port - 端口号。
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 需要在打印服务连接成功后调用此函数，建议在getAllPrinters调用成功的回调接口中调用该接口，保证传入的打印机名称和端口的打印机状态正常。
 * 注意：此函数仅能连接 USB 打印机列表中的打印机。
 */
function selectPrinter(printerName, port, callbackFunction = null) {
  var jsonObj = {
    apiName: 'selectPrinter',
    parameter: { printerName: printerName, port: port }
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 发送消息以选择打印机。
 *
 * @param {string} printerName - 打印机名称。
 * @param {number} port - 端口号。
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 需要在打印服务连接成功后调用此函数，建议在scanWifiPrinter调用成功的回调接口中调用该接口，保证传入的打印机名称和端口的打印机状态正常。
 * 注意：此函数仅能连接 WIFI 打印机列表中的打印机。
 */
function connectWifiPrinter(printerName, port, callbackFunction = null) {
  timeout_duration = 25000;
  var jsonObj = {
    apiName: 'connectWifiPrinter',
    parameter: { printerName: printerName, port: port }
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 发送消息以断开打印机。
 *
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 可以断开USB和WIFI连接的打印机
 */
function closePrinter(callbackFunction = null) {
  var jsonObj = {
    apiName: 'closePrinter',
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 配置打印机的Wifi网络
 * 
 * @param {string} wifiName - wifi网络的名称。
 * @param {string} wifiPassword - wifi网络的密码。
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 * 
 * @description
 * 注意:仅支持2.4G频段网络，且需要在连接成功后配置。需要在USB连接成功后配置
 */
function configurationWifi(wifiName, wifiPassword, callbackFunction = null) {
  timeout_duration = 25000;
  var jsonObj = {
    apiName: 'configurationWifi',
    parameter: { wifiName: wifiName, wifiPassword: wifiPassword }
  }
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 获取打印机的wifi配置。
 *
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 */
function getWifiConfiguration(callbackFunction = null) {
  var jsonObj = {
    apiName: 'getWifiConfiguration'
  }
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}



/**
 * 初始化绘制画板
 *
 * @param {Object} json - 包含初始化绘制画板所需数据的JSON对象。格式如下：
 * {
 *   "width": number, // 画板的宽度，单位为mm
 *   "height": number, // 画板的高度，单位为mm
 *   "rotate": number, // 画板的旋转角度，仅支持0、90、180、270
 *   "path": string, // 字体文件的路径，默认为""，暂不生效
 *   "verticalShift": number, // 垂直偏移量，暂不生效
 *   "horizontalShift": number // 水平偏移量，暂不生效
 * }
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 增加接口说明:
 * 1. 在调用绘制接口之前，必须先初始化SDK。
 * 2. 绘制元素前，必须先初始化画板，否则会引起崩溃！
 * 3. 初始化画板时会清空画板上次绘制的内容！
 */
function InitDrawingBoard(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'InitDrawingBoard',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制标签文本。
 * @param {object} json - 包含标签文本信息的JSON对象。
 *   JSON格式要求如下：
 *   - x: x轴坐标，单位mm
 *   - y: y轴坐标，单位mm
 *   - height: 文本高度，单位mm
 *   - width: 文本宽度，单位mm
 *   - value: 文本内容
 *   - fontFamily: 字体名称，暂不生效，使用默认字体思源黑体
 *   - rotate: 旋转角度，仅支持0、90、180、270
 *   - fontSize: 字号，单位mm
 *   - textAlignHorizonral: 水平对齐方式：0:左对齐 1:居中对齐 2:右对齐
 *   - textAlignVertical: 垂直对齐方式：0:顶对齐 1:垂直居中 2:底对齐
 *   - letterSpacing: 字母之间的标准间隔，单位mm
 *   - lineSpacing: 行间距（倍距），默认1
 *   - lineMode: 1:宽高固定，内容大小自适应，预设宽高过大时字号放大，预设宽高过小时字号缩小，
 *     保证内容占据满预设宽高（字号/字符间距/行间距 按比例缩放）
 *     2:宽度固定，高度自适应  
 *     4:宽高固定,超出内容直裁切
 *     6:宽高固定，内容超过预设的文本宽高自动缩放
 *     建议设置为6
 *   - fontStyle: 字体样式[加粗，斜体，下划线，删除下划线（预留）]
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 * @description 绘制标签文本前必须先初始化画板
 */
function DrawLableText(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableText',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制一维码条形码。
 *
 * @param {Object} json - 包含一维码条形码信息的JSON对象。格式如下：
 * {
 *   "x": number, // x轴坐标，单位mm
 *   "y": number, // y轴坐标，单位mm
 *   "height": number, // 一维码宽度，单位mm
 *   "width": number, // 一维码高度，单位mm（包含文本高度）
 *   "value": string, // 一维码内容
 *   "codeType": number, // 条码类型：
 *                     // 20: CODE128
 *                     // 21: UPC-A
 *                     // 22: UPC-E
 *                     // 23: EAN8
 *                     // 24: EAN13
 *                     // 25: CODE93
 *                     // 26: CODE39
 *                     // 27: CODEBAR
 *                     // 28: ITF25
 *   "rotate": number, // 旋转角度，仅支持0、90、180、270
 *   "fontSize": number, // 文本字号，单位mm，字号为0则文本不显示
 *   "textHeight": number, // 文本高度，单位mm，高度为0则文本不显示
 *   "textPosition": number // 一维码文字识别码显示位置：
 *                          // 0: 下方显示
 *                          // 1: 上方显示
 *                          // 2: 不显示
 * }
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 1. 绘制元素前，必须先初始化画板
 */
function DrawLableBarCode(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableBarCode',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制二维码。
 *
 * @param {Object} json - 包含二维码信息的JSON对象。格式如下：
 * {
 *   "x": number, // x轴坐标，单位mm
 *   "y": number, // y轴坐标，单位mm
 *   "height": number, // 二维码高度，默认宽高一致
 *   "width": number, // 二维码宽度，单位mm
 *   "value": string, // 二维码内容
 *   "codeType": number, // 条码类型：
 *                     // 31: QR_CODE
 *                     // 32: PDF417
 *                     // 33: DATA_MATRIX
 *                     // 34: AZTEC
 *   "rotate": number, // 旋转角度，仅支持0、90、180、270
 * }
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 1. 绘制元素前，必须先初始化画板
 */
function DrawLableQrCode(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableQrCode',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制带logo的二维码。
 * @param {*} json - 包含二维码信息的JSON对象。格式如下：
 * {
 *   "x": number, // x轴坐标，单位mm
 *   "y": number, // y轴坐标，单位mm
 *   "height": number, // 二维码高度，默认宽高一致
 *   "width": number, // 二维码宽度，单位mm
 *   "value": string, // 二维码内容
 *   "codeType": number, // 条码类型：
 *                     // 31: QR_CODE
 *                     // 32: PDF417
 *                     // 33: DATA_MATRIX
 *                     // 34: AZTEC
 *   "rotate": number, // 旋转角度，仅支持0、90、180、270
 *  "correctLevel": 2,//纠错级别，取值范围1-4，默认2
 *  ""logoBase64": ": string,//logo的base64编码(不含数据头，如data:image/png;base64,)
 *  ""logoPosition": ": 0,//logo的位置，取值范围0-4，默认0:居中，3右下·
 *  "logoHeight": number,//logo高度，单位mm，默认10mm
 *    "logoScale": 0.25,//logo缩放比例，取值范围0-0.33，默认0.25
 * }
 * @param {*} callbackFunction - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 1. 绘制元素前，必须先初始化画板
 */
function DrawLableQrCodeWithLogo(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableQrCodeWithImage',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制线条。
 *
 * @param {Object} json - 包含线条信息的JSON对象。格式如下：
 * {
 *   "x": number, // x轴坐标，单位mm
 *   "y": number, // y轴坐标，单位mm
 *   "height": number, // 线高，单位mm
 *   "width": number, // 线宽，单位mm
 *   "lineType": number, // 线条类型：1:实线 2:虚线类型,虚实比例1:1
 *   "rotate": number, // 旋转角度，仅支持0、90、180、270
 *   "dashwidth": number // 线条为虚线宽度，【实线段长度，空线段长度】
 * }
 * @param {Function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 1. 绘制元素前，必须先初始化画板
 */
function DrawLableLine(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableLine',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制图形。
 *
 * @param {Object} json - 包含绘制图形信息的JSON对象。格式如下：
 * {
 *   "x": number, // x轴坐标，单位mm
 *   "y": number, // y轴坐标，单位mm
 *   "height": number, // 图形高度，单位mm
 *   "width": number, // 图形宽度，单位mm
 *   "rotate": number, // 旋转角度，仅支持0、90、180、270
 *   "cornerRadius": number, // 圆角半径，单位mm，暂不生效
 *   "lineWidth": number, // 线宽，单位mm
 *   "lineType": number, // 线条类型：1:实线 2:虚线类型,虚实比例1:1
 *   "graphType": number, // 图形类型：1:圆，2:椭圆，3:矩形 4:圆角矩形
 *   "dashwidth": number // 线条为虚线宽度，【实线段长度，空线段长度】
 * }
 * @param {Function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 1. 绘制元素前，必须先初始化画板
 */
function DrawLableGraph(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableGraph',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 绘制图片。
 *
 * @param {Object} json - 包含绘制图片信息的JSON对象。格式如下：
 * {
 *   "x": number, // x轴坐标，单位mm
 *   "y": number, // y轴坐标，单位mm
 *   "height": number, // 图片高度，单位mm
 *   "width": number, // 图片宽度，单位mm
 *   "rotate": number, // 旋转角度，仅支持0、90、180、270
 *   "imageProcessingType": number, // 图像处理算法，默认0
 *   "imageProcessingValue": number, // 算法参数，默认127
 *   "imageData": number, // 图片base64数据，不含数据头
 *                     // 如原始数据为“data:image/png;base64,iVBORw0KGgoAAAANSU”
 *                     // 传入的数据需要去除头部，数据为，“iVBORw0KGgoAAAANSU”
 * }
 * @param {Function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 增加接口说明:
 * 1. 绘制元素前，必须先初始化画板
 */
function DrawLableImage(json, callbackFunction = null) {
  var jsonObj = {
    apiName: 'DrawLableImage',
    parameter: json
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 生成图像预览图像。
 *
 * @param {number} displayScale - 图像显示比例，表示 1mm 的点数，可调整预览图大小。
 *                               例如，200dpi 的打印机可设置为 8，300dpi 的打印机可设置为 11.81。
 * @param {Function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 *
 * @description
 * 增加方法说明:
 * 1. 在调用此函数之前，必须确保图像数据已准备好，否则无法生成预览。
 */
function generateImagePreviewImage(displayScale, callbackFunction = null) {
  var jsonObj = {
    apiName: 'generateImagePreviewImage',
    displayScale: displayScale
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 启动打印任务。
 * 
 * @param {number} printDensity - 打印浓度，根据不同打印机型号取值范围不同，具体如下：
 *   - B1、B21、B21S、B21_Pro、B203、B3S、B3S_P、B31、B4、K2、K3、K3W、M2、M3: 取值范围 1~5，默认为 3。
 *   - B50、B11、B50W、B32、Z401、B32R: 取值范围 1~15，默认为 8。
 * @param {number} paperType - 纸张类型，可选值：
 *   1: 间隙纸
 *   2: 黑标纸
 *   3: 连续纸
 *   4: 定孔纸
 *   5: 透明纸
 *   6: 标牌
 *   10: 黑标间隙纸
 * @param {string} printMode - 打印模式，可选值：
 *   1: 热敏
 *   2: 热转印
 *   注意，不同打印机型号支持的打印模式有限制，具体如下：
 *   - B1、B21、B21S、B21_Pro、B203、B3S、B3S_P、B31、B4、K2、K3、K3W、B11 仅支持热敏。
 *   - B50、B50W、B32、Z401、B32R、M2、M3 仅支持热转印。
 * @param {number} count - 总打印份数，表示所有页面的打印份数之和。
 *   例如，如果你有3页需要打印，第一页打印3份，第二页打印2份，第三页打印5份，那么count的值应为10（3+2+5）。
 * @param {Function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 * @example
 * //返回数据示例
 * {
 *     "apiName": "startJob",
 *     "resultAck": {
 *         "errorCode": 0,
 *         "info": "startJob ok!",
 *         "result": 0
 *     }
 * }
 * @description 返回结果中的 errorCode 含义如下：
 *              - 0: 成功
 *              - -1: 失败，info 表示原因
 *              - -2: 打印机忙碌，info 表示原因
 *              - -3: 打印机接收到不支持的参数，主要是浓度、纸张类型、打印模式，info 表示具体原因
 * @return {undefined} 此函数不返回任何值。
 * 
 */
function startJob(printDensity, printLabelType, printMode, count, callbackFunction = null) {
  var jsonObj = {
    apiName: 'startJob',
    parameter: {
      printDensity: printDensity,
      printLabelType: printLabelType,
      printMode: printMode,
      count: count
    }
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}


/**
 * 提交打印任务，并执行回调函数。
 *
 * @param {string} [printData=null] - 打印数据的 JSON 字符串。
 * @param {string} printerImageProcessingInfo - 打印机图像处理信息的 JSON 字符串，包含打印份数信息，格式如下：
 * {
 *   "printerImageProcessingInfo": {
 *     "printQuantity": 1 // 用于指定当前页的打印份数。例如，如果需要打印3页，第一页打印3份，第二页打印2份，第三页打印5份，则在3次提交数据时，printerImageProcessingInfo 中的 "printQuantity" 值分别应为 3，2，5。
 *   }
 * }
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 * 
 * @description
 * 需要先开启打印任务，完成绘制后再提交打印任务
 */
function commitJob(printData, printerImageProcessingInfo, callbackFunction = null) {
  // 解析 printDataJson，如果解析失败则使用空对象
  var printDataJson = parseJsonSafely(printData);

  // 解析 printerImageProcessingInfoJson，如果解析失败则使用空对象
  var printerImageProcessingInfoJson = parseJsonSafely(printerImageProcessingInfo);

  // 构建提交作业的参数对象
  var jsonObj = {
    apiName: 'commitJob',
    parameter: {
      printData: printDataJson,
      printerImageProcessingInfo: printerImageProcessingInfoJson['printerImageProcessingInfo'],
    }
  };

  // 调用 sendMsg 函数发送消息并执行回调函数
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 结束打印任务
 * 
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 * @description
 * 收到最后一页最后一份打印页面后调用该函数结束打印任务
 */
function endJob(callbackFunction = null) {
  var jsonObj = { apiName: 'endJob' };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 取消当前的打印任务，并执行回调函数。
 *
 * @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
 * @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
 * @description
 * 调用该函数后，打印机将立即停止当前的打印任务。
 */
function cancelJob(callbackFunction = null) {
  var jsonObj = { apiName: 'stopPrint' };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
* 设置打印机的自动关机时间。（该函数暂时被移除无法使用）
*
* @param {number} nType - 自动关机时间的类型：
*   1: 15分钟，
*   2: 30分钟，
*   3: 60分钟，
*   4: 从不
* @param {function} [callbackFunction] - 可选的回调函数；传入则使用回调返回结果，不传则返回 Promise。
* @return {Promise|undefined} 不传 callbackFunction 时返回 Promise，否则无返回值。
*/
function setPrinterAutoShutDownTime(nType, callbackFunction) {
  var jsonObj = {
    apiName: 'setPrinterAutoShutDownTime',
    parameter: { nType: nType }
  };
  // [修改] 增加 return
  return sendMsg(jsonObj, callbackFunction);
}

/**
 * 添加打印任务监听器（用于 commitJob 等）
 * 监听器将在每次收到 commitJob 主动上报时被调用，参数为打印服务回包的完整 JSON。
 * 
 * @param {Function} callbackFunction - 接收打印任务进度、异常、允许提交数据的回调函数
 * @returns {Function|undefined} 添加成功返回回调函数本身，参数不合法时返回 undefined
 */
function addJobListener(callbackFunction) {
  if (typeof callbackFunction !== "function") return;
  // 确保去重：先删除后添加
  jobListeners.delete(callbackFunction);
  jobListeners.add(callbackFunction);
  return callbackFunction;
}

/**
 * 移除打印任务监听器
 * 移除后该回调不再接收 commitJob 的主动上报。
 * 
 * @param {Function} callbackFunction - 待移除的回调函数引用
 * @returns {void}
 */
function removeJobListener(callbackFunction) {
  if (!callbackFunction) return;
  jobListeners.delete(callbackFunction);
}


/**
 * 添加打印机硬件状态监听器 (用于 printStatus, 缺纸, 开盖等)
 * 这类监听器通常在连接成功后添加，断开后移除，不会随单次打印任务销毁
 */
function addPrinterStatusListener(callbackFunction) {
  if (typeof callbackFunction !== "function") return;
  statusListeners.delete(callbackFunction);
  statusListeners.add(callbackFunction);
  return callbackFunction;
}

/**
 * 移除打印机硬件状态监听器
 */
function removePrinterStatusListener(callbackFunction) {
  if (!callbackFunction) return;
  statusListeners.delete(callbackFunction);
}



// 调试用
function getListenersCount() {
  return {
    job: jobListeners.size,
    status: statusListeners.size
  };
}

/**
 * 将字符串解析为 JSON 对象，如果解析失败则返回空对象
 * @param {string} jsonString - 待解析的 JSON 字符串
 * @returns {object} - 解析得到的 JSON 对象
 */
function parseJsonSafely(jsonString) {
  try {
    return JSON.parse(jsonString) || null;
  } catch (error) {
    return null;
  }
}

/**
 * 在一定次数的尝试后从 API 获取结果。
 *
 * @param {number} tryTime - 尝试获取结果的次数。
 * @param {string} apiName - API 的名称。
 * @param {string} errInfo - 错误信息。
 * @return {object} 包含获取到的结果的对象。
 */
function getResult(tryTime, apiName, errInfo) {
  tryTimes = tryTime;

  let result = {};
  while (tryTimes--) {
    if (!isJSON(ackJsonData)) continue;

    var arrParse = JSON.parse(ackJsonData);
    if (arrParse['apiName'] === apiName) {
      result = arrParse['resultAck'];
      break;
    }
  }

  if (tryTimes <= 0) {
    result['result'] = false;
    result['errorCode'] = 0x12;
    result['info'] = errInfo;
  }
  return result;
}

/**
* 检查字符串是否为JSON格式。
*
* @param {string} str - 要检查的字符串。
* @returns {boolean} 如果字符串是JSON格式的，则返回true，否则返回false。
*/
function isJSON(str) {
  if (typeof str == 'string') {
    try {
      var obj = JSON.parse(str);
      if (typeof obj == 'object' && obj) {
        return true;
      } else {
        return false;
      }

    } catch (e) {
      //console.log('error：'+str+'!!!'+e);
      return false;
    }

  }

  // console.log('It is not a string!'); // [修改] 减少日志
}
