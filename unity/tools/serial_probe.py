#!/usr/bin/env python3
"""
Small STM32 dispenser serial probe.

Examples:
  python tools/serial_probe.py --list
  python tools/serial_probe.py --port COM3 clean
  python tools/serial_probe.py --port COM3 open_tray
  python tools/serial_probe.py --port COM3 --repeat 5 --interval 1 clean
  python tools/serial_probe.py --port COM3 raw 02
"""

from __future__ import annotations

import argparse
import sys
import time
from typing import Iterable

serial = None
list_ports = None


COMMANDS = {
    "skip": 0x00,
    "reset": 0x01,
    "clean": 0x02,
    "open_tray": 0x03,
    "close_tray": 0x04,
}


def build_package(command: int, data: bytes = b"") -> bytes:
    payload = bytes([command]) + data
    crc = sum(payload) & 0xFFFF
    return b"\xAA\xBB" + payload + bytes([crc & 0xFF, (crc >> 8) & 0xFF])


def parse_hex_bytes(hex_text: str) -> bytes:
    cleaned = hex_text.replace("0x", "").replace("0X", "")
    cleaned = "".join(ch for ch in cleaned if ch not in " -_:,")
    if len(cleaned) % 2:
        raise ValueError(f"Hex string must have an even number of digits: {hex_text}")
    return bytes.fromhex(cleaned)


def require_pyserial() -> None:
    global serial, list_ports

    if serial is not None and list_ports is not None:
        return

    try:
        import serial as serial_module
        from serial.tools import list_ports as list_ports_module
    except ImportError:
        print("Missing dependency: pyserial", file=sys.stderr)
        print("Install with: python -m pip install pyserial", file=sys.stderr)
        raise SystemExit(2)

    serial = serial_module
    list_ports = list_ports_module


def print_ports() -> None:
    require_pyserial()
    ports = list(list_ports.comports())
    if not ports:
        print("No serial ports found.")
        return

    for port in ports:
        desc = port.description or ""
        hwid = port.hwid or ""
        print(f"{port.device}\t{desc}\t{hwid}")


def read_for(ser: serial.Serial, duration: float) -> bytes:
    deadline = time.monotonic() + duration
    chunks: list[bytes] = []

    while time.monotonic() < deadline:
        waiting = ser.in_waiting
        if waiting:
            chunks.append(ser.read(waiting))
            continue

        chunk = ser.read(1)
        if chunk:
            chunks.append(chunk)
        else:
            time.sleep(0.01)

    return b"".join(chunks)


def print_received(data: bytes) -> None:
    if not data:
        print("RX: <no data>")
        return

    print(f"RX HEX: {data.hex(' ').upper()}")
    text = data.decode("ascii", errors="replace")
    print(f"RX TXT: {text!r}")

    lines = [line.strip() for line in text.replace("\r", "\n").split("\n") if line.strip()]
    for line in lines:
        marker = ""
        if line in {"ACK", "DONE"}:
            marker = "  <-- protocol marker"
        elif line.startswith(("machine_state:", "pills out:", "cleaned pills:", "number:")):
            marker = "  <-- feedback"
        print(f"RX LINE: {line}{marker}")


def make_packet(args: argparse.Namespace) -> bytes:
    if args.command == "raw":
        return parse_hex_bytes(args.hex)

    command = COMMANDS[args.command]
    return build_package(command)


def open_serial(args: argparse.Namespace):
    require_pyserial()
    return serial.Serial(
        port=args.port,
        baudrate=args.baud,
        bytesize=serial.EIGHTBITS,
        parity=serial.PARITY_NONE,
        stopbits=serial.STOPBITS_ONE,
        timeout=args.read_timeout,
        write_timeout=args.write_timeout,
        dsrdtr=False,
        rtscts=False,
    )


def run_probe(args: argparse.Namespace) -> int:
    packet = make_packet(args)

    print(f"Opening {args.port} @ {args.baud} 8N1")
    with open_serial(args) as ser:
        ser.dtr = False
        ser.rts = False
        ser.reset_input_buffer()
        ser.reset_output_buffer()

        if args.settle > 0:
            print(f"Waiting {args.settle:.2f}s after opening port...")
            time.sleep(args.settle)
            startup = read_for(ser, 0.1)
            if startup:
                print("Startup data:")
                print_received(startup)

        for index in range(args.repeat):
            if args.repeat > 1:
                print(f"\nAttempt {index + 1}/{args.repeat}")

            print(f"TX HEX: {packet.hex(' ').upper()}")
            ser.write(packet)
            ser.flush()

            data = read_for(ser, args.wait)
            print_received(data)

            if index < args.repeat - 1:
                time.sleep(args.interval)

    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Probe STM32 dispenser serial protocol.")
    parser.add_argument("--list", action="store_true", help="List available serial ports and exit.")
    parser.add_argument("--port", help="COM port, for example COM3.")
    parser.add_argument("--baud", type=int, default=115200, help="Baud rate. Default: 115200.")
    parser.add_argument("--read-timeout", type=float, default=0.05, help="Serial read timeout seconds.")
    parser.add_argument("--write-timeout", type=float, default=0.2, help="Serial write timeout seconds.")
    parser.add_argument("--wait", type=float, default=3.0, help="Seconds to wait for feedback after each write.")
    parser.add_argument("--settle", type=float, default=0.2, help="Seconds to wait after opening the port.")
    parser.add_argument("--repeat", type=int, default=1, help="Number of times to send the packet.")
    parser.add_argument("--interval", type=float, default=0.5, help="Seconds between repeated sends.")

    subparsers = parser.add_subparsers(dest="command")
    for name in COMMANDS:
        subparsers.add_parser(name, help=f"Send {name} command.")

    raw = subparsers.add_parser("raw", help="Send raw hex bytes, for example: AA BB 02 02 00")
    raw.add_argument("hex", help="Raw packet bytes as hex.")

    parser.set_defaults(command="clean")
    return parser


def main(argv: Iterable[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.list:
        print_ports()
        return 0

    if not args.port:
        parser.error("--port is required unless --list is used")

    require_pyserial()

    try:
        return run_probe(args)
    except serial.SerialException as exc:
        print(f"Serial error: {exc}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nInterrupted.")
        return 130


if __name__ == "__main__":
    raise SystemExit(main())
