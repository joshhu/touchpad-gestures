# touchpad-gestures

[English](README.md) | 繁體中文

Windows 11 精確式觸控板手勢：用手勢開啟程式，並依照目前所在的視窗做不同動作。另附一個小型常駐程式（`CloseActive.exe`），負責需要判斷視窗的那幾個手勢。

| 手勢 | 快速鍵 | 動作 |
|---|---|---|
| 三指右滑 | Ctrl+Alt+T | 在 Windows Terminal 開啟 PowerShell 7 |
| 三指上滑 | Ctrl+Alt+C | 在 Chrome 中：開新分頁；其他程式：開啟 Chrome |
| 三指左滑 | Ctrl+Alt+E | 開啟檔案總管並進入家目錄 |
| 四指點選 | Ctrl+Alt+W | 在 Chrome 中：關閉分頁；其他程式：關閉視窗（桌面與工作列不動作） |

這些設定會取代 Windows 原本的手勢（切換應用程式、工作檢視、通知中心），改用 Alt+Tab、Win+Tab、Win+N 代替。

## 硬體與系統需求

- **Windows 11**：自訂捷徑手勢是 Windows 11 才有的功能。
- **精確式觸控板（Windows Precision Touchpad）**：不限廠牌，Surface、Dell、Lenovo、HP、ASUS 等近年筆電大多支援。
  - 確認方式：「設定 → 藍牙與裝置 → 觸控板」裡有「進階手勢」可設定三指、四指自訂捷徑，就能使用。
  - 使用廠商專屬驅動程式的舊式觸控板（如部分 Synaptics、Elan）沒有進階手勢，無法使用手勢部分。
- **沒有觸控板也能用**：四組快速鍵直接用鍵盤按一樣有效。
- x64 與 arm64 皆可（`CloseActive.exe` 用 Windows 內建的 .NET Framework 4.x 編譯）。
- 需要 PowerShell 7、Windows Terminal，以及安裝在預設路徑的 Google Chrome。

## 運作方式

Windows 的手勢只能對應到固定的一組按鍵，所以每個手勢都是送出一組快速鍵：

- **Ctrl+Alt+T / Ctrl+Alt+E**：開始功能表捷徑（`.lnk`）上設定的快速鍵。
- **Ctrl+Alt+W / Ctrl+Alt+C**：由 `CloseActive.exe` 以 `RegisterHotKey` 註冊。它會判斷目前最上層的視窗，在 Chrome 中送出 Ctrl+W / Ctrl+T，其他情況則關閉視窗或開啟 Chrome。這兩組不能用 `.lnk` 快速鍵，因為檔案總管在啟動程式前會把焦點移到工作列，程式就抓不到你原本所在的視窗。

## 安裝

```powershell
git clone https://github.com/joshhu/touchpad-gestures
cd touchpad-gestures
pwsh -File .\install.ps1
```

會把 `CloseActive.exe` 編譯到 `%LOCALAPPDATA%\Programs\CloseActive`、建立捷徑、加入「啟動」資料夾，並把手勢設定寫入 `HKCU\Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad`。如果手勢沒有反應，登出再登入一次。

想自己設定手勢的話，加上 `-SkipGestures` 執行，再到「設定 → 藍牙與裝置 → 觸控板 → 進階手勢」，把各手勢設為「自訂捷徑」並錄製對應的快速鍵。

## 解除安裝

1. 刪除「啟動」資料夾裡的 `CloseActive.lnk`，並結束 `CloseActive.exe`。
2. 刪除 `%APPDATA%\Microsoft\Windows\Start Menu\Programs` 裡的 `PowerShell 7 (Terminal).lnk` 與 `Explorer Home (Hotkey).lnk`。
3. 刪除 `%LOCALAPPDATA%\Programs\CloseActive`。
4. 到「設定 → 觸控板 → 進階手勢」把手勢改回預設。

## 授權

MIT
