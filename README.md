# Helper

**Helper** is a Windows application developed in C# (.NET Framework 4.6.1) intended to work alongside the `SystemInFor` Windows service. While `SystemInFor` runs in the background as a system-level service, `Helper` is launched in the user's session to perform operations that require user context, such as interacting with the desktop, managing windows, or tracking user activity.

---

## ⚙️ Features

- Runs as a background app in user session
- Automatically starts with Windows (via registry key)
- Works in conjunction with `SystemInFor` to trigger user-level actions
- Lightweight and minimal footprint

---

## 🧱 Technologies

- **Language:** C#
- **Framework:** .NET Framework 4.6.1
- **Project Type:** Windows Forms Application or Console App (depending on implementation)

---

## 📁 Project Structure

```
Helper/
├── Helper/                → Source code (.csproj)
├── bin/Release/           → Compiled Helper.exe
├── README.md
```

---

## 🚀 Usage

`Helper.exe` is not designed to be run directly by the user.
Instead, it is started automatically on user login by the `SystemInFor` service using a registry key:

```bash
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SystemInForHelper
```

Once started, it waits for commands or watches specific system activity, depending on your implementation.

---

## ⚠️ Requirements

- Must reside in the same folder as `SystemInFor.exe`
- .NET Framework 4.6.1 must be installed
- Installed via `SystemInForInstaller.exe` (see SystemInFor repository)

---

## 👤 Author

- **Name:** Miloš Jovanović  
- **GitHub:** [milostjov](https://github.com/milostjov)

---

## 📄 License

This project is licensed under the **MIT License**.

```text
MIT License

Copyright (c) 2025 Miloš Jovanović

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

