using System.Collections.Generic;

namespace sy_ftp.Resources;

public static class Strings
{
    public static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        // Toolbar
        ["toolbar.connect"] = "Connect",
        ["toolbar.connecting"] = "Connecting…",
        ["toolbar.connect.tooltip"] = "Connect to selected host",
        ["toolbar.disconnect"] = "Disconnect",
        ["toolbar.disconnect.tooltip"] = "Disconnect",
        ["toolbar.refresh"] = "Refresh",
        ["toolbar.refresh.tooltip"] = "Refresh file list",
        ["toolbar.theme.tooltip"] = "Toggle light / dark theme",
        ["toolbar.topmost.tooltip"] = "Always on top",
        ["toolbar.settings.tooltip"] = "Settings",

        // Sidebar
        ["sidebar.hosts"] = "Hosts",
        ["sidebar.add"] = "+ Add",
        ["sidebar.add.tooltip"] = "Add new host",
        ["sidebar.delete"] = "Delete",
        ["sidebar.delete.tooltip"] = "Delete selected host",
        ["sidebar.accent"] = "Accent",
        ["sidebar.tag.all"] = "All tags",

        // Host context menu
        ["host.menu.connect"] = "Connect",
        ["host.menu.disconnect"] = "Disconnect",
        ["host.menu.edit"] = "Edit Host",
        ["host.menu.delete"] = "Delete Host",
        ["host.menu.add"] = "Add New Host",

        // File browser
        ["file.col.name"] = "Name",
        ["file.col.size"] = "Size",
        ["file.col.modified"] = "Modified",
        ["file.empty.title"] = "Select a host and click Connect",
        ["file.items"] = "{0} items",
        ["file.synced"] = "Synced {0}",
        ["file.path.copy.tooltip"] = "Copy path",
        ["file.path.copied"] = "Path copied!",
        ["file.path.edit.go.tooltip"] = "Go (Enter)",
        ["file.path.edit.cancel.tooltip"] = "Cancel (Esc)",
        ["file.tip.connected"] = "Connected",

        // File item context menu
        ["file.menu.refresh"] = "Refresh",
        ["file.menu.new_folder"] = "New Folder",
        ["file.menu.new_file"] = "New File",
        ["file.menu.download"] = "Download",
        ["file.menu.download_to"] = "Download to…",
        ["file.menu.remote_edit"] = "Remote Edit",
        ["file.menu.online_edit"] = "Online Edit",
        ["file.menu.transfer_to"] = "Transfer to…",
        ["file.menu.delete"] = "Delete",

        // Status bar
        ["status.disconnected"] = "Disconnected",
        ["status.connecting"] = "Connecting...",
        ["status.connected"] = "Connected to {0}",
        ["status.error"] = "Error: {0}",

        // Download progress
        ["download.single"] = "Downloading {0}...",
        ["download.single.pct"] = "Downloading {0}... {1:F0}%",
        ["download.multi.label"] = "{0} ({1}/{2})",
        ["download.done.single"] = "Downloaded {0}",
        ["download.done.multi"] = "Downloaded {0} items",
        ["download.choose_folder"] = "Choose download folder",

        // Dialogs — Host edit
        ["hostedit.add.title"] = "Add Host",
        ["hostedit.edit.title"] = "Edit Host",
        ["hostedit.header"] = "Host",
        ["hostedit.field.name"] = "Name",
        ["hostedit.field.name.placeholder"] = "My FTP Server",
        ["hostedit.field.name.required"] = "Name is required",
        ["hostedit.field.host"] = "Host",
        ["hostedit.field.host.placeholder"] = "ftp.example.com",
        ["hostedit.field.host.required"] = "Host address is required",
        ["hostedit.field.port"] = "Port",
        ["hostedit.field.username"] = "Username",
        ["hostedit.field.username.placeholder"] = "anonymous",
        ["hostedit.field.password"] = "Password",
        ["hostedit.field.password.placeholder"] = "Enter password",
        ["hostedit.field.tags"] = "Tags",
        ["hostedit.field.tags.placeholder"] = "prod, web",
        ["hostedit.field.download_path"] = "Download path",
        ["hostedit.field.download_path.placeholder"] = "Leave empty to use default",
        ["hostedit.field.download_path.browse"] = "Browse…",
        ["hostedit.btn.cancel"] = "Cancel",
        ["hostedit.btn.save"] = "Save",

        // Dialogs — Input
        ["input.new_folder.title"] = "New Folder",
        ["input.new_folder.label"] = "Folder name",
        ["input.new_file.title"] = "New File",
        ["input.new_file.label"] = "File name",
        ["input.btn.cancel"] = "Cancel",
        ["input.btn.ok"] = "OK",
        ["input.error.required"] = "Name cannot be empty",

        // Dialogs — Confirm
        ["confirm.title"] = "Confirm",
        ["confirm.btn.cancel"] = "Cancel",
        ["confirm.delete.title"] = "Confirm delete",
        ["confirm.delete.btn"] = "Delete",
        ["confirm.delete.host.title"] = "Delete host",
        ["confirm.delete.host.msg"] = "Delete host \"{0}\"? This cannot be undone.",
        ["confirm.delete.single"] = "Delete \"{0}\"? This cannot be undone.",
        ["confirm.delete.multi"] = "Delete {0} items? This cannot be undone.",

        // Errors
        ["error.remote_edit"] = "Remote edit failed: {0}",
        ["error.online_edit"] = "Online edit failed: {0}",
        ["error.source_not_connected"] = "Source host is not connected.",
        ["error.watcher_invalid"] = "Connection lost — this edit session is invalid. Reconnect and open the file again.",
        ["error.upload_failed"] = "Upload failed — connection may have dropped. Reconnect and open the file again.",

        // Transfer dialog
        ["transfer.title"] = "Transfer to",
        ["transfer.destination"] = "Destination",
        ["transfer.host.placeholder"] = "Select a host...",
        ["transfer.btn.connect"] = "Connect",
        ["transfer.btn.disconnect"] = "Disconnect",
        ["transfer.empty"] = "Pick a destination host, then click Connect",
        ["transfer.connecting"] = "Connecting...",
        ["transfer.btn.close"] = "Close",
        ["transfer.btn.transfer"] = "Transfer here",
        ["transfer.tooltip.up"] = "Up one level",
        ["transfer.tooltip.refresh"] = "Refresh",

        // Remote edit
        ["remoteedit.btn.cancel"] = "Cancel",
        ["remoteedit.btn.save"] = "Save",
        ["remoteedit.close.tooltip"] = "Close without saving",

        // Settings window
        ["settings.title"] = "Settings",
        ["settings.section.general"] = "General",
        ["settings.section.appearance"] = "Appearance",
        ["settings.section.paths"] = "Paths",
        ["settings.language"] = "Language",
        ["settings.language.en"] = "English",
        ["settings.language.zh"] = "中文 (Chinese)",
        ["settings.theme"] = "Theme",
        ["settings.theme.light"] = "Light",
        ["settings.theme.dark"] = "Dark",
        ["settings.accent"] = "Accent color",
        ["settings.accent.hint"] = "Pick any color to customize the app accent.",
        ["settings.path.download"] = "Default download path",
        ["settings.path.download.hint"] = "New downloads go here when the selected host has no custom path.",
        ["settings.path.data"] = "Default data path",
        ["settings.path.data.hint"] = "Where settings and host config are stored. Restart required.",
        ["settings.path.browse"] = "Browse…",
        ["settings.path.reset"] = "Reset",
        ["settings.btn.close"] = "Close",
    };

    public static readonly IReadOnlyDictionary<string, string> Zh = new Dictionary<string, string>
    {
        // Toolbar
        ["toolbar.connect"] = "连接",
        ["toolbar.connecting"] = "连接中…",
        ["toolbar.connect.tooltip"] = "连接选中的主机",
        ["toolbar.disconnect"] = "断开",
        ["toolbar.disconnect.tooltip"] = "断开连接",
        ["toolbar.refresh"] = "刷新",
        ["toolbar.refresh.tooltip"] = "刷新文件列表",
        ["toolbar.theme.tooltip"] = "切换浅色 / 深色主题",
        ["toolbar.topmost.tooltip"] = "窗口置顶",
        ["toolbar.settings.tooltip"] = "设置",

        // Sidebar
        ["sidebar.hosts"] = "主机",
        ["sidebar.add"] = "+ 新增",
        ["sidebar.add.tooltip"] = "新增主机",
        ["sidebar.delete"] = "删除",
        ["sidebar.delete.tooltip"] = "删除选中主机",
        ["sidebar.accent"] = "强调色",
        ["sidebar.tag.all"] = "全部标签",

        // Host context menu
        ["host.menu.connect"] = "连接",
        ["host.menu.disconnect"] = "断开",
        ["host.menu.edit"] = "编辑主机",
        ["host.menu.delete"] = "删除主机",
        ["host.menu.add"] = "新增主机",

        // File browser
        ["file.col.name"] = "名称",
        ["file.col.size"] = "大小",
        ["file.col.modified"] = "修改时间",
        ["file.empty.title"] = "请选择主机并点击连接",
        ["file.items"] = "共 {0} 项",
        ["file.synced"] = "已同步 {0}",
        ["file.path.copy.tooltip"] = "复制路径",
        ["file.path.copied"] = "路径已复制",
        ["file.path.edit.go.tooltip"] = "前往 (Enter)",
        ["file.path.edit.cancel.tooltip"] = "取消 (Esc)",
        ["file.tip.connected"] = "已连接",

        // File item context menu
        ["file.menu.refresh"] = "刷新",
        ["file.menu.new_folder"] = "新建文件夹",
        ["file.menu.new_file"] = "新建文件",
        ["file.menu.download"] = "下载",
        ["file.menu.download_to"] = "下载到…",
        ["file.menu.remote_edit"] = "远程编辑",
        ["file.menu.online_edit"] = "在线编辑",
        ["file.menu.transfer_to"] = "传输到…",
        ["file.menu.delete"] = "删除",

        // Status bar
        ["status.disconnected"] = "未连接",
        ["status.connecting"] = "连接中...",
        ["status.connected"] = "已连接到 {0}",
        ["status.error"] = "错误:{0}",

        // Download progress
        ["download.single"] = "正在下载 {0}...",
        ["download.single.pct"] = "正在下载 {0}... {1:F0}%",
        ["download.multi.label"] = "{0} ({1}/{2})",
        ["download.done.single"] = "已下载 {0}",
        ["download.done.multi"] = "已下载 {0} 个项目",
        ["download.choose_folder"] = "选择下载文件夹",

        // Dialogs — Host edit
        ["hostedit.add.title"] = "新增主机",
        ["hostedit.edit.title"] = "编辑主机",
        ["hostedit.header"] = "主机",
        ["hostedit.field.name"] = "名称",
        ["hostedit.field.name.placeholder"] = "我的 FTP 服务器",
        ["hostedit.field.name.required"] = "名称不能为空",
        ["hostedit.field.host"] = "主机地址",
        ["hostedit.field.host.placeholder"] = "ftp.example.com",
        ["hostedit.field.host.required"] = "主机地址不能为空",
        ["hostedit.field.port"] = "端口",
        ["hostedit.field.username"] = "用户名",
        ["hostedit.field.username.placeholder"] = "anonymous",
        ["hostedit.field.password"] = "密码",
        ["hostedit.field.password.placeholder"] = "请输入密码",
        ["hostedit.field.tags"] = "标签",
        ["hostedit.field.tags.placeholder"] = "生产, web",
        ["hostedit.field.download_path"] = "下载路径",
        ["hostedit.field.download_path.placeholder"] = "留空使用全局默认",
        ["hostedit.field.download_path.browse"] = "浏览…",
        ["hostedit.btn.cancel"] = "取消",
        ["hostedit.btn.save"] = "保存",

        // Dialogs — Input
        ["input.new_folder.title"] = "新建文件夹",
        ["input.new_folder.label"] = "文件夹名",
        ["input.new_file.title"] = "新建文件",
        ["input.new_file.label"] = "文件名",
        ["input.btn.cancel"] = "取消",
        ["input.btn.ok"] = "确定",
        ["input.error.required"] = "名称不能为空",

        // Dialogs — Confirm
        ["confirm.title"] = "确认",
        ["confirm.btn.cancel"] = "取消",
        ["confirm.delete.title"] = "确认删除",
        ["confirm.delete.btn"] = "删除",
        ["confirm.delete.host.title"] = "删除主机",
        ["confirm.delete.host.msg"] = "确定要删除主机 \"{0}\" 吗?此操作不可撤销。",
        ["confirm.delete.single"] = "确定要删除 \"{0}\" 吗?此操作不可撤销。",
        ["confirm.delete.multi"] = "确定要删除这 {0} 项吗?此操作不可撤销。",

        // Errors
        ["error.remote_edit"] = "远程编辑失败:{0}",
        ["error.online_edit"] = "在线编辑失败:{0}",
        ["error.source_not_connected"] = "源主机未连接。",
        ["error.watcher_invalid"] = "连接已断开 — 此编辑会话已失效。请重新连接后再次打开文件。",
        ["error.upload_failed"] = "上传失败 — 连接可能已断开。请重新连接后再次打开文件。",

        // Transfer dialog
        ["transfer.title"] = "传输到",
        ["transfer.destination"] = "目标",
        ["transfer.host.placeholder"] = "选择主机...",
        ["transfer.btn.connect"] = "连接",
        ["transfer.btn.disconnect"] = "断开",
        ["transfer.empty"] = "先选择目标主机,然后点击连接",
        ["transfer.connecting"] = "连接中...",
        ["transfer.btn.close"] = "关闭",
        ["transfer.btn.transfer"] = "传输到此处",
        ["transfer.tooltip.up"] = "返回上一级",
        ["transfer.tooltip.refresh"] = "刷新",

        // Remote edit
        ["remoteedit.btn.cancel"] = "取消",
        ["remoteedit.btn.save"] = "保存",
        ["remoteedit.close.tooltip"] = "不保存并关闭",

        // Settings window
        ["settings.title"] = "设置",
        ["settings.section.general"] = "通用",
        ["settings.section.appearance"] = "外观",
        ["settings.section.paths"] = "路径",
        ["settings.language"] = "语言",
        ["settings.language.en"] = "English",
        ["settings.language.zh"] = "中文",
        ["settings.theme"] = "主题",
        ["settings.theme.light"] = "浅色",
        ["settings.theme.dark"] = "深色",
        ["settings.accent"] = "强调色",
        ["settings.accent.hint"] = "挑选任意颜色来自定义应用的强调色。",
        ["settings.path.download"] = "默认下载路径",
        ["settings.path.download.hint"] = "当主机未设置专属路径时,下载保存到这里。",
        ["settings.path.data"] = "默认数据存储路径",
        ["settings.path.data.hint"] = "设置和主机配置的存储位置。修改后需要重启应用。",
        ["settings.path.browse"] = "浏览…",
        ["settings.path.reset"] = "重置",
        ["settings.btn.close"] = "关闭",
    };
}
