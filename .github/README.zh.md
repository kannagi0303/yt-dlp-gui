# yt-dlp-gui

* [yt-dlp](https://github.com/yt-dlp/yt-dlp) 與 [youtube-dl](https://github.com/ytdl-org/youtube-dl) 的前端GUI
* 作業系統 Windows 限定

### 特色
* 簡單使用
* 可攜式

### 截圖
<img src="screenshot03.png" width="460"/>

### 執行需求
* [yt-dlp](https://github.com/yt-dlp/yt-dlp) (建議) 或 [youtube-dl](https://github.com/ytdl-org/youtube-dl)
* [FFMPEG](https://ffmpeg.org/download.html#build-windows)

[yt-dlp](https://github.com/yt-dlp/yt-dlp) 是基於 [youtube-dl](https://github.com/ytdl-org/youtube-dl) 的分支，
使用命令列指令從 Youtube 與 其他影片網站 下載影片，
[yt-dlp](https://github.com/yt-dlp/yt-dlp) 專案增加許多功能與修補，同時也保持與原始項目的更新。

### 可選
* [aria2](https://aria2.github.io/)

### 如何使用 (建議步驟)
1. 下載 [`yt-dlp-gui.exe`](https://github.com/kannagi0303/yt-dlp-gui/releases)
2. 於執行檔位置建立`bin`資料夾
3. 將`yt-dlp.exe`以及`ffmpeg.exe`複製進`bin`資料夾中
4. 執行 `yt-dlp-gui.exe`

* 首次執行將會產生`yt-dlp-gui.yaml`檔案，用來存放設定。
* `2022.10.03`或之後版本, `yt-dlp.exe` & `ffmpeg.exe` 可放置與`yt-dlp-gui.exe`相同位置, 或是`yt-dlp-gui.exe`之下**任何名稱**的資料夾 (即使巢狀)

#### 使用Configuration設定 (參考 [configuration](https://github.com/yt-dlp/yt-dlp#configuration))
1. 於執行檔位置建立`configs`資料夾
2. 將 configuration 檔案放至`configs`資料夾中 (純文字)

* 需要重啟掃描configuration設定檔案

#### 使用 Aria2 (建議步驟)
1. 如果 `yt-dlp-gui.exe` 在實行中請先關閉
2. 於`yt-dlp-gui.exe`執行檔位置建立`bin`資料夾
3. 將`aria2c.exe`複製進`bin`資料夾中

* 需要重啟掃描`aria2c.exe`執行檔
* `2022.10.03`或之後版本, `aria2c.exe` 可放置與`yt-dlp-gui.exe`相同位置, 或是`yt-dlp-gui.exe`之下**任何名稱**的資料夾 (即使巢狀)

### 如何刪除
直接刪除 `yt-dlp-gui.exe` 即可

### 作者
* かんなぎ (Kannagi)

由於無法找到適合自己使用的GUI介面，
為了方便使用 yt-dlp, 所以粗略的自己來寫，
使用的是 C# 與 WPF, 基本的使用已無大問題，
有任何問題可以通知我，我將盡速處理，
有建議與問題也歡迎反映給我, 主要為中文或簡單的英文, 同時日文也是可以
