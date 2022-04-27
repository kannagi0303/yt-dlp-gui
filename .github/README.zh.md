# yt-dlp-gui

* [yt-dlp](https://github.com/yt-dlp/yt-dlp) 的前端GUI
* 作業系統 Windows 限定

[yt-dlp](https://github.com/yt-dlp/yt-dlp) 是基於 [youtube-dl](https://github.com/ytdl-org/youtube-dl) 的分支，
使用命令列指令從 Youtube 與 其他影片網站 下載影片，
[yt-dlp](https://github.com/yt-dlp/yt-dlp) 專案增加許多功能與修補，同時也保持與原始項目的更新。



### 截圖
<img src="screenshot01.png" width="460"/>

### 執行需求
* [yt-dlp](https://github.com/yt-dlp/yt-dlp)
* [FFMPEG](https://ffmpeg.org/download.html#build-windows)

### 如何使用
1. 下載以及解壓縮
2. 於執行檔位置建立`bin`資料夾
3. 將`yt-dlp.exe`以及`ffmpeg.exe`複製進`bin`資料夾中
4. 執行`yt-dlp-gui.exe`

### 作者
* かんなぎ (Kannagi)
 
由於無法找到適合自己使用的GUI介面, 
為了方便使用 yt-dlp, 所以粗略的自己來寫, 
使用的是 C# 與 WPF, 基本的使用已無大問題
    
由於是第一次發佈在github, 有許多部份不熟悉, 日後在整理與放出源碼
有建議與問題也歡迎反映給我, 主要為中文或簡單的英文, 同時日文也是可以