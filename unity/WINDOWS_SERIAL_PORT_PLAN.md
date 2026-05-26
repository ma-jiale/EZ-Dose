# Windows Serial Port Implementation Plan

## 1. Goal

Develop a Windows version of the dispenser controller app in the existing Unity project.

The Windows version will connect directly to the STM32 controller through a COM serial port. The existing Android version will continue to use the Android Bluetooth serial plugin.

The communication protocol must stay unchanged:

- Unity still sends the same byte packages built by `SerialProtocol`.
- STM32 still returns the same text feedback, such as `ACK`, `DONE`, `machine_state:FINISH`, `pills out:`, and `cleaned pills:`.
- Dispensing flow, UI flow, patient data logic, pill counting logic, and calibration logic should not change unless required by platform differences.

The main difference between Android and Windows should be only the transport layer:

- Android: Bluetooth serial plugin.
- Windows: Direct STM32 serial port, for example `COM3`, `COM4`, etc.

## 2. Current Code Shape

Important files:

- `Assets/Scripts/DispenserController.cs`
  - Owns hardware state.
  - Sends commands to STM32.
  - Receives feedback.
  - Handles connection state.
  - Currently directly depends on `AndroidJavaObject` and the Android Bluetooth plugin.

- `Assets/Scripts/SerialProtocol.cs`
  - Defines command bytes.
  - Builds command packages.
  - Parses STM32 feedback messages.
  - Should remain platform independent.

- `Assets/Scripts/DeviceManagerUI.cs`
  - Shows the device management dialog.
  - Starts device discovery.
  - Renders device cards.
  - Calls `DispenserController.ConnectToDevice(...)`.

- `Assets/Scripts/DeviceCardUI.cs`
  - Displays one device card.
  - Shows connect/disconnect buttons.

- `Assets/Scripts/BluetoothDeviceInfo.cs`
  - Represents discovered Bluetooth devices.
  - Can be reused temporarily for Windows COM ports to minimize UI changes.

## 3. Recommended Architecture

Introduce a transport abstraction so `DispenserController` no longer talks directly to Android Bluetooth or Windows serial APIs.

Create a new folder:

```text
Assets/Scripts/Hardware/Transport/
```

Add these files:

```text
IDispenserTransport.cs
AndroidBluetoothTransport.cs
WindowsSerialTransport.cs
EditorMockTransport.cs
```

### 3.1 IDispenserTransport

The interface should provide the operations needed by `DispenserController`:

```csharp
public interface IDispenserTransport
{
    bool IsConnected { get; }

    bool Connect(string connectionId);
    void Disconnect();

    bool Write(byte[] data);
    string Read();

    List<BluetoothDeviceInfo> DiscoverDevices();
}
```

For Windows, `connectionId` is the COM port name, for example `COM3`.

For Android, `connectionId` is the Bluetooth MAC address.

## 4. Windows Serial Transport Design

### 4.1 Runtime API

Use:

```csharp
System.IO.Ports.SerialPort
```

Add conditional compilation so the class only compiles for Windows:

```csharp
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
```

### 4.2 Default Serial Settings

Recommended defaults:

```text
Baud rate: 115200
Data bits: 8
Parity: None
Stop bits: One
Read timeout: 50 ms
Write timeout: 200 ms
NewLine: "\n"
```

Confirm the baud rate with the STM32 firmware. If the firmware uses a different rate, update the Unity setting.

### 4.3 Serial Discovery

Use:

```csharp
SerialPort.GetPortNames()
```

Each discovered port should become one device card:

```text
DeviceName: STM32 Dispenser (COM3)
MacAddress: COM3
IsPaired: true
SignalStrength: -1
```

This reuses `BluetoothDeviceInfo` even though the field `MacAddress` will contain a COM port. This is acceptable for the first implementation because it avoids UI refactoring.

Later, rename `BluetoothDeviceInfo` to a platform-neutral type such as `DispenserDeviceInfo`.

### 4.4 Connect

Windows connection logic:

```csharp
serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
serialPort.ReadTimeout = readTimeoutMs;
serialPort.WriteTimeout = writeTimeoutMs;
serialPort.NewLine = "\n";
serialPort.Open();
```

After successful connection:

```text
IsConnected = true
```

### 4.5 Write

Use the same byte package built by `SerialProtocol`:

```csharp
serialPort.Write(data, 0, data.Length);
```

No protocol conversion should be added.

### 4.6 Read

Read available text from the serial port:

```csharp
string data = serialPort.ReadExisting();
```

Pass the result to existing parsing logic:

```csharp
ProcessReceivedData(data);
```

The existing parser already handles line-based messages.

### 4.7 Disconnect

Close and dispose the serial port:

```csharp
if (serialPort != null)
{
    if (serialPort.IsOpen)
    {
        serialPort.Close();
    }

    serialPort.Dispose();
    serialPort = null;
}
```

## 5. Android Bluetooth Transport Design

Move the current Android plugin code out of `DispenserController` and into `AndroidBluetoothTransport`.

This class should keep using:

```csharp
AndroidJavaObject bluetoothSerial;
```

It should preserve existing behavior:

- Create `com.unity.bluetooth.BluetoothSerial`.
- Check Bluetooth availability.
- Check Bluetooth enabled state.
- Connect by MAC address.
- Discover paired devices.
- Write byte arrays using `writeBytes`.
- Read using `read`.
- Disconnect using `disconnect`.

Important: Android behavior should remain unchanged after this refactor.

## 6. Editor Mock Transport Design

Keep a mock transport for testing when no hardware is connected.

Two reasonable options:

1. For Windows Editor, use the real Windows serial transport.
2. For non-Windows Editor, use a mock transport.

Recommended:

```csharp
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    transport = new WindowsSerialTransport(...);
#elif UNITY_ANDROID && !UNITY_EDITOR
    transport = new AndroidBluetoothTransport(...);
#else
    transport = new EditorMockTransport();
#endif
```

This allows development on Windows Editor against a real STM32 COM port.

## 7. DispenserController Refactor Steps

### Step 1: Add Transport Field

Add:

```csharp
private IDispenserTransport transport;
```

Keep existing state fields:

```csharp
isConnected
isReceiving
isSendingPackage
ackReceived
doneReceived
connectedDevice
discoveredDevices
```

### Step 2: Create Transport Factory Method

Add:

```csharp
private IDispenserTransport CreateTransport()
```

Platform selection:

```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
    return new AndroidBluetoothTransport();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    return new WindowsSerialTransport(windowsBaudRate, readTimeoutMs, writeTimeoutMs);
#else
    return new EditorMockTransport();
#endif
```

### Step 3: Replace ConnectBluetooth

Rename `ConnectBluetooth()` to `ConnectTransport()` or keep the old method name temporarily.

Current:

```csharp
private bool ConnectBluetooth()
```

New behavior:

```csharp
if (transport == null)
{
    transport = CreateTransport();
}

bool connected = transport.Connect(deviceMacAddress);
isConnected = connected;
return connected;
```

For Windows, `deviceMacAddress` will temporarily store the COM port.

### Step 4: Replace Disconnect

Current Android-specific disconnect code should move into `AndroidBluetoothTransport`.

`DispenserController.Disconnect()` should call:

```csharp
transport?.Disconnect();
```

Then keep existing cleanup:

```csharp
isConnected = false;
ResetState();
```

### Step 5: Replace StartDeviceDiscovery

Current Android discovery code and editor simulation should move into transports.

New `StartDeviceDiscovery()`:

```csharp
discoveredDevices.Clear();
OnDiscoveryStarted?.Invoke();

try
{
    var devices = transport.DiscoverDevices();
    discoveredDevices.AddRange(devices);
    OnDevicesFound?.Invoke(new List<BluetoothDeviceInfo>(discoveredDevices));
}
catch (Exception e)
{
    OnBTError?.Invoke(e.Message);
}
finally
{
    OnDiscoveryCompleted?.Invoke();
}
```

If discovery should remain animated, wrap it in a coroutine with a short delay.

### Step 6: Replace ReceiveDataCoroutine

Current Android-specific read:

```csharp
bluetoothSerial.Call<string>("read")
```

New:

```csharp
string data = transport.Read();
if (!string.IsNullOrEmpty(data))
{
    ProcessReceivedData(data);
}
```

Keep the existing receive frequency:

```csharp
yield return new WaitForSeconds(0.05f);
```

### Step 7: Replace SendBytes

Current Android-specific write:

```csharp
bluetoothSerial.Call<bool>("writeBytes", sdata)
```

New:

```csharp
return transport != null && transport.Write(data);
```

Keep all existing retry and ACK logic in `SendPackageCoroutine`.

### Step 8: Update Heartbeat

Current Android heartbeat checks plugin state.

Windows first version:

- Check `transport.IsConnected`.
- If false, call `HandleConnectionLost(...)`.
- If `Write(...)` throws or returns false during normal commands, existing send failure logic will also handle disconnect.

Avoid adding a new STM32 ping command unless firmware already supports one.

## 8. Device Management UI Changes

The current UI flow should remain:

```text
Show dialog
StartDeviceScan()
DispenserController.StartDeviceDiscovery()
OnDevicesFound(...)
CreateDeviceCard(...)
Click connect
DispenserController.ConnectToDevice(...)
```

### Step 1: Keep Device Cards

Do not remove the card UI.

Each COM port appears as one card.

Example:

```text
STM32 Dispenser (COM3)
COM3
[Connect]
```

### Step 2: Neutralize Text

Replace Bluetooth-specific UI text with neutral device text.

Examples:

```text
正在扫描可用设备...
发现 3 个设备
没有发现设备
正在连接 COM3...
已连接 COM3
已断开连接
```

### Step 3: Keep Existing Events

Keep using:

```csharp
OnDiscoveryStarted
OnDevicesFound
OnDiscoveryCompleted
OnConnectionStateChanged
OnBTError
```

Optional later rename:

```text
OnBTError -> OnTransportError
```

For the first version, keep old event names to reduce diff size.

## 9. Suggested Implementation Timeline

### Phase 1: Preparation

Estimated time: 0.5 day

Tasks:

- Confirm STM32 serial parameters.
- Confirm Windows can see STM32 as a COM port in Device Manager.
- Confirm Unity Windows build target is installed.
- Confirm `System.IO.Ports` is available with current Unity API Compatibility settings.
- Create a small standalone serial probe if needed.

Acceptance:

- STM32 appears as `COMx`.
- Baud rate is known.
- Unity can compile a script referencing `System.IO.Ports` on Windows.

### Phase 2: Transport Interface

Estimated time: 0.5 day

Tasks:

- Add `IDispenserTransport`.
- Add folder `Assets/Scripts/Hardware/Transport/`.
- Add `EditorMockTransport`.
- Do not change behavior yet.

Acceptance:

- Project compiles.
- No existing Android behavior changed.

### Phase 3: Android Transport Extraction

Estimated time: 1 day

Tasks:

- Move Android plugin connection code into `AndroidBluetoothTransport`.
- Move Android discovery code into `AndroidBluetoothTransport`.
- Move Android read/write/disconnect code into `AndroidBluetoothTransport`.
- Make `DispenserController` call transport methods.

Acceptance:

- Android build still compiles.
- Android Bluetooth discovery still works.
- Android Bluetooth connection still works.
- Existing commands still send and receive ACK.

### Phase 4: Windows Serial Transport

Estimated time: 1 day

Tasks:

- Implement `WindowsSerialTransport`.
- Use `SerialPort.GetPortNames()` for discovery.
- Use `SerialPort.Open()` for connect.
- Use `SerialPort.Write(...)` for write.
- Use `SerialPort.ReadExisting()` for read.
- Use `Close()` and `Dispose()` for disconnect.

Acceptance:

- Windows Editor shows COM ports in the device management dialog.
- Clicking a COM port connects without crashing.
- Disconnect closes the COM port so it can be reopened.

### Phase 5: Device Management UI Polish

Estimated time: 0.5 day

Tasks:

- Replace Bluetooth-specific text with neutral device/serial text.
- Keep card layout unchanged.
- Ensure connected card state updates correctly.
- Ensure refresh button rescans COM ports.

Acceptance:

- Multiple COM ports show as multiple cards.
- Connected COM port shows disconnect state.
- Refresh after unplug/replug updates the list.

### Phase 6: Command-Level Hardware Test

Estimated time: 1 day

Test commands in this order:

1. Connect to COM port.
2. Send `CLEAN_PILLS`.
3. Confirm STM32 receives command.
4. Confirm Unity receives `ACK`.
5. Confirm Unity receives `DONE`.
6. Confirm Unity receives `cleaned pills:xx` if firmware sends it.
7. Test `OPEN_TRAY`.
8. Test `CLOSE_TRAY`.
9. Test `SET_MOTOR_SPEED`.
10. Test `RESET_DISPENSER`.

Acceptance:

- Commands are sent successfully.
- ACK wait logic works.
- DONE wait logic works.
- No UI freeze during serial communication.

### Phase 7: Full Dispensing Flow Test

Estimated time: 1-2 days

Tasks:

- Select patient.
- Scan pill box if needed.
- Start dispensing.
- Send pill matrix.
- Receive progress feedback.
- Pause/resume.
- Skip current medicine.
- Clean turntable from Home button.
- Clean turntable from Skip dialog button.
- Complete dispensing.

Acceptance:

- Full flow works on Windows with STM32 serial.
- Android build remains unaffected.

### Phase 8: Windows Build Packaging

Estimated time: 0.5 day

Tasks:

- Switch Unity build target to Windows.
- Build Windows standalone.
- Run executable on target Windows machine.
- Confirm COM port access from built app.
- Confirm logs are available for troubleshooting.

Acceptance:

- Windows executable starts.
- Device management dialog lists COM ports.
- User can connect and operate dispenser.

## 10. Testing Checklist

### Discovery

- [ ] No STM32 connected: UI shows no device.
- [ ] One STM32 connected: UI shows one COM port card.
- [ ] Multiple serial devices connected: UI shows multiple cards.
- [ ] Refresh updates the list after plug/unplug.

### Connection

- [ ] Connect to valid COM port succeeds.
- [ ] Connect to occupied COM port fails gracefully.
- [ ] Disconnect releases COM port.
- [ ] Reconnect after disconnect succeeds.

### Send/Receive

- [ ] `CLEAN_PILLS` sends bytes.
- [ ] ACK is received.
- [ ] DONE is received.
- [ ] Timeout path works when STM32 does not respond.
- [ ] Connection lost path works when USB is unplugged.

### UI

- [ ] Device card connect/disconnect button updates correctly.
- [ ] Home device button shows connected/disconnected state.
- [ ] Main Home clean button works only when connected.
- [ ] Skip dialog clean button works only when connected.
- [ ] Old skip clean checkbox behavior is gone.

### Regression

- [ ] Android Bluetooth discovery still works.
- [ ] Android Bluetooth connection still works.
- [ ] Android dispensing flow still works.

## 11. Risks and Mitigations

### Risk: `System.IO.Ports` is unavailable or incompatible in Unity build

Mitigation:

- Confirm API Compatibility setting.
- Test compile early.
- If unavailable, add a serial plugin or use a lightweight native Windows serial wrapper.

### Risk: COM port is occupied by another program

Mitigation:

- Catch exceptions from `SerialPort.Open()`.
- Show a clear error in UI.
- Allow refresh/retry.

### Risk: STM32 resets when opening serial port

Some boards reset when the serial port opens because of DTR/RTS behavior.

Mitigation:

- Test whether STM32 resets on connect.
- If needed, configure:

```csharp
serialPort.DtrEnable = false;
serialPort.RtsEnable = false;
```

or add a short connection delay before sending first command.

### Risk: Binary command bytes and text feedback share the same serial link

This is already true for the current protocol.

Mitigation:

- Keep outgoing binary writes unchanged.
- Keep incoming text line parsing unchanged.
- Do not add encoding conversion to outgoing command packets.

### Risk: Multiple COM ports but user does not know which one is STM32

Mitigation:

- First version: show all COM ports.
- Later version: add optional filtering by USB VID/PID using Windows APIs or a plugin.
- Later version: add a firmware handshake command to identify the dispenser.

## 12. First Milestone

The first milestone should be small and hardware-focused:

```text
Windows Editor
Device management dialog
List COM ports
Connect to selected COM port
Send CLEAN_PILLS
Receive ACK and DONE
Disconnect cleanly
```

Do not start by testing the full dispensing flow. First prove the serial transport path works.

## 13. Final Desired Result

The final result should be:

- One Unity project.
- Android build uses Bluetooth serial plugin.
- Windows build uses STM32 COM serial port.
- Same STM32 command protocol.
- Same dispenser UI.
- Same patient and dispensing workflow.
- Device management dialog remains, but lists serial ports on Windows.
