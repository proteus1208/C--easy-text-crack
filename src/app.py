# --- ALLOWED_MAC_SECTION ---
_A = ""
_B = ""
# --- END_ALLOWED_MAC_SECTION ---

import hashlib
import os
import re
import subprocess


def run():
    return 0


def main():
    if not _allow():
        raise SystemExit(0)


def _allow():
    macs = _local_macs()
    if not macs or not _A or not _B:
        return False
    for mac in macs:
        digest = _digest(mac)
        if digest == _A and _sign(digest) == _B:
            return True
    return False


def _digest(mac):
    hex_digits = re.sub(r"[^0-9A-Fa-f]", "", mac).upper()
    return _mix(_key(1), hex_digits.encode("ascii"))


def _sign(digest):
    return _mix(_key(2), digest.encode("ascii"))


def _mix(key, data):
    return hashlib.sha256(key + data).hexdigest()


def _key(which):
    if which == 1:
        mixed = bytes((
            0xFF, 0xAD, 0x3B, 0x62, 0x96, 0x28,
            0x57, 0xEE, 0x04, 0xCC, 0x25, 0x4B,
        ))
        mask = 0x3C
    else:
        mixed = bytes((
            0x74, 0x02, 0xEE, 0x57, 0x39, 0xC0,
            0xBB, 0x1F, 0xD6, 0x7B, 0xAC, 0x2A,
        ))
        mask = 0x5A
    return bytes(b ^ mask for b in mixed)


def _local_macs():
    try:
        output = subprocess.check_output(
            ["getmac"],
            stderr=subprocess.DEVNULL,
            stdin=subprocess.DEVNULL,
            creationflags=0x08000000,
        )
    except Exception:
        return set()
    if not isinstance(output, str):
        output = output.decode("ascii", "ignore")
    found = set()
    for match in re.findall(r"(?:[0-9A-Fa-f]{2}[-:]){5}[0-9A-Fa-f]{2}", output):
        found.add(re.sub(r"[^0-9A-Fa-f]", "", match).upper())
    return found


if __name__ == "__main__":
    main()
