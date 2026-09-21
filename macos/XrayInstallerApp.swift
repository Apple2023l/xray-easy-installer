import AppKit
import CoreImage.CIFilterBuiltins
import SwiftUI
import UniformTypeIdentifiers

enum AppLanguage: String, CaseIterable, Identifiable {
    case russian, english, persian, chinese
    var id: String { rawValue }
    var name: String {
        switch self { case .russian: "Русский"; case .english: "English"; case .persian: "فارسی"; case .chinese: "中文" }
    }
}

private let localized: [AppLanguage: [String: String]] = [
    .russian: [
        "language":"Язык", "installTab":"Установка", "podkopTab":"Роутер · JSON", "shadowTab":"Телефон · QR", "title":"Xray Installer", "subtitle":"VLESS + REALITY или Hysteria 2 · %@",
        "ip":"IP сервера", "login":"SSH-логин", "password":"SSH-пароль", "passwordHint":"Пароль", "profile":"Профиль", "site":"Сайт REALITY", "masqueradeSite":"Сайт маскировки", "domain":"Домен Hysteria 2",
        "xhttp":"XHTTP + REALITY — основной", "vision":"RAW + REALITY + Vision — быстрый", "hysteria":"Hysteria 2 + Salamander — UDP 40460", "install":"Подключиться и установить",
        "ready":"Введите данные нового сервера.", "connecting":"Подключение к %@…\n", "installing":"Установка сервера…", "done":"Готово — ссылка создана и проверена.", "badDomain":"Введите домен, направленный на IP сервера.",
        "failed":"Установка не завершилась. Подробности в журнале.", "badIP":"Введите корректный IPv4-адрес.", "badLogin":"Введите корректный SSH-логин.",
        "noPassword":"Введите SSH-пароль.", "missingFiles":"В приложении отсутствуют файлы установки.", "launchError":"Ошибка запуска: %@", "error":"Ошибка: %@",
        "profileReady":"Профиль готов", "scan":"Сканируйте QR-код в клиенте или скопируйте ссылку.", "newLink":"Новая ссылка", "copyLink":"Скопировать ссылку",
        "saveQR":"Сохранить QR…", "progress":"Ход установки", "podkopTitle":"Конфигуратор Podkop", "podkopSubtitle":"VLESS или Hysteria 2 → sing-box outbound JSON",
        "vlessLink":"Ссылка подключения", "xhttpMode":"Режим XHTTP", "compatible":"packet-up — совместимый", "visionInfo":"Vision использует прямой RAW/TCP транспорт", "hysteriaInfo":"Hysteria 2 использует UDP и Salamander",
        "generate":"Сформировать outbound", "copyJSON":"Скопировать JSON", "pasteFirst":"Вставьте ссылку или сначала установите сервер.",
        "badLink":"Нужна корректная ссылка VLESS или Hysteria 2.", "realityOnly":"Поддерживается профиль VLESS + REALITY с sni, pbk и sid.",
        "profileRequired":"Нужен профиль XHTTP или RAW/TCP с flow=xtls-rprx-vision.", "podkopDone":"Готово. Вставьте JSON в Podkop → Proxy → Outbound Config.",
        "jsonError":"Не удалось сформировать JSON: %@", "xhttpHelp":"Для XHTTP нужен sing-box со встроенной поддержкой XHTTP. В Podkop выбирайте Outbound Config.",
        "visionHelp":"Vision передаётся через flow=xtls-rprx-vision. В Podkop выбирайте Outbound Config.", "shadowTitle":"Импорт в Shadowrocket",
        "shadowSubtitle":"Вставьте ссылку вручную или отсканируйте QR-код", "copyVless":"Скопировать ссылку", "shadowHelp":"Используйте сканер QR или импорт ссылки из буфера",
        "noProfile":"Нет профиля", "noProfileHelp":"Вставьте ссылку или создайте сервер на первой вкладке."
    ],
    .english: [
        "language":"Language", "installTab":"Install", "podkopTab":"Router · JSON", "shadowTab":"Phone · QR", "title":"Xray Installer", "subtitle":"VLESS + REALITY or Hysteria 2 · %@",
        "ip":"Server IP", "login":"SSH username", "password":"SSH password", "passwordHint":"Password", "profile":"Profile", "site":"REALITY site", "masqueradeSite":"Camouflage site", "domain":"Hysteria 2 domain",
        "xhttp":"XHTTP + REALITY — primary", "vision":"RAW + REALITY + Vision — fast", "hysteria":"Hysteria 2 + Salamander — UDP 40460", "install":"Connect and install",
        "ready":"Enter the new server details.", "connecting":"Connecting to %@…\n", "installing":"Installing the server…", "done":"Done — the link was created and verified.", "badDomain":"Enter a domain that points to the server IP.",
        "failed":"Installation did not complete. See the log for details.", "badIP":"Enter a valid IPv4 address.", "badLogin":"Enter a valid SSH username.",
        "noPassword":"Enter the SSH password.", "missingFiles":"Installation files are missing from the app.", "launchError":"Launch error: %@", "error":"Error: %@",
        "profileReady":"Profile ready", "scan":"Scan the QR code in your client or copy the link.", "newLink":"New link", "copyLink":"Copy link",
        "saveQR":"Save QR…", "progress":"Installation progress", "podkopTitle":"Podkop configurator", "podkopSubtitle":"VLESS or Hysteria 2 → sing-box outbound JSON",
        "vlessLink":"Connection link", "xhttpMode":"XHTTP mode", "compatible":"packet-up — compatible", "visionInfo":"Vision uses direct RAW/TCP transport", "hysteriaInfo":"Hysteria 2 uses UDP and Salamander",
        "generate":"Generate outbound", "copyJSON":"Copy JSON", "pasteFirst":"Paste a link or install a server first.",
        "badLink":"A valid VLESS or Hysteria 2 link is required.", "realityOnly":"A VLESS + REALITY profile with sni, pbk, and sid is required.",
        "profileRequired":"An XHTTP or RAW/TCP profile with flow=xtls-rprx-vision is required.", "podkopDone":"Ready. Paste the JSON into Podkop → Proxy → Outbound Config.",
        "jsonError":"Could not generate JSON: %@", "xhttpHelp":"XHTTP requires sing-box with built-in XHTTP support. Select Outbound Config in Podkop.",
        "visionHelp":"Vision is passed through flow=xtls-rprx-vision. Select Outbound Config in Podkop.", "shadowTitle":"Import into Shadowrocket",
        "shadowSubtitle":"Paste the link or scan the QR code", "copyVless":"Copy link", "shadowHelp":"Use the QR scanner or import the link from the clipboard",
        "noProfile":"No profile", "noProfileHelp":"Paste a link or create a server on the first tab."
    ],
    .persian: [
        "language":"زبان", "installTab":"نصب", "podkopTab":"روتر · JSON", "shadowTab":"تلفن · QR", "title":"نصب‌کننده Xray", "subtitle":"VLESS + REALITY یا Hysteria 2 · %@",
        "ip":"IP سرور", "login":"نام کاربری SSH", "password":"رمز عبور SSH", "passwordHint":"رمز عبور", "profile":"پروفایل", "site":"سایت REALITY", "masqueradeSite":"سایت پوششی", "domain":"دامنه Hysteria 2",
        "xhttp":"XHTTP + REALITY — اصلی", "vision":"RAW + REALITY + Vision — سریع", "hysteria":"Hysteria 2 + Salamander — UDP 40460", "install":"اتصال و نصب",
        "ready":"اطلاعات سرور جدید را وارد کنید.", "connecting":"در حال اتصال به %@…\n", "installing":"در حال نصب سرور…", "done":"انجام شد — لینک ساخته و بررسی شد.", "badDomain":"دامنه‌ای را وارد کنید که به IP سرور اشاره کند.",
        "failed":"نصب کامل نشد. جزئیات را در گزارش ببینید.", "badIP":"یک آدرس IPv4 معتبر وارد کنید.", "badLogin":"نام کاربری معتبر SSH را وارد کنید.",
        "noPassword":"رمز عبور SSH را وارد کنید.", "missingFiles":"فایل‌های نصب در برنامه وجود ندارند.", "launchError":"خطای اجرا: %@", "error":"خطا: %@",
        "profileReady":"پروفایل آماده است", "scan":"کد QR را اسکن کنید یا لینک را کپی کنید.", "newLink":"لینک جدید", "copyLink":"کپی لینک",
        "saveQR":"ذخیره QR…", "progress":"روند نصب", "podkopTitle":"پیکربندی Podkop", "podkopSubtitle":"VLESS یا Hysteria 2 → فایل JSON برای sing-box",
        "vlessLink":"لینک اتصال", "xhttpMode":"حالت XHTTP", "compatible":"packet-up — سازگار", "visionInfo":"Vision از انتقال مستقیم RAW/TCP استفاده می‌کند", "hysteriaInfo":"Hysteria 2 از UDP و Salamander استفاده می‌کند",
        "generate":"ساخت Outbound", "copyJSON":"کپی JSON", "pasteFirst":"لینک را وارد کنید یا ابتدا سرور را نصب کنید.",
        "badLink":"لینک معتبر VLESS یا Hysteria 2 لازم است.", "realityOnly":"پروفایل VLESS + REALITY با sni، pbk و sid لازم است.",
        "profileRequired":"پروفایل XHTTP یا RAW/TCP با flow=xtls-rprx-vision لازم است.", "podkopDone":"آماده است. JSON را در Podkop → Proxy → Outbound Config وارد کنید.",
        "jsonError":"ساخت JSON ناموفق بود: %@", "xhttpHelp":"XHTTP به sing-box با پشتیبانی داخلی XHTTP نیاز دارد. در Podkop گزینه Outbound Config را انتخاب کنید.",
        "visionHelp":"Vision از flow=xtls-rprx-vision استفاده می‌کند. در Podkop گزینه Outbound Config را انتخاب کنید.", "shadowTitle":"ورود به Shadowrocket",
        "shadowSubtitle":"لینک را وارد کنید یا کد QR را اسکن کنید", "copyVless":"کپی لینک", "shadowHelp":"از اسکنر QR یا ورود لینک از کلیپ‌بورد استفاده کنید",
        "noProfile":"پروفایلی وجود ندارد", "noProfileHelp":"لینک را وارد کنید یا در زبانه اول یک سرور بسازید."
    ],
    .chinese: [
        "language":"语言", "installTab":"安装", "podkopTab":"路由器 · JSON", "shadowTab":"手机 · 二维码", "title":"Xray 安装器", "subtitle":"VLESS + REALITY 或 Hysteria 2 · %@",
        "ip":"服务器 IP", "login":"SSH 用户名", "password":"SSH 密码", "passwordHint":"密码", "profile":"配置模式", "site":"REALITY 站点", "masqueradeSite":"伪装站点", "domain":"Hysteria 2 域名",
        "xhttp":"XHTTP + REALITY — 首选", "vision":"RAW + REALITY + Vision — 高速", "hysteria":"Hysteria 2 + Salamander — UDP 40460", "install":"连接并安装",
        "ready":"请输入新服务器信息。", "connecting":"正在连接 %@…\n", "installing":"正在安装服务器…", "done":"完成 — 链接已创建并验证。", "badDomain":"请输入指向服务器 IP 的域名。",
        "failed":"安装未完成，请查看日志。", "badIP":"请输入有效的 IPv4 地址。", "badLogin":"请输入有效的 SSH 用户名。",
        "noPassword":"请输入 SSH 密码。", "missingFiles":"应用中缺少安装文件。", "launchError":"启动错误：%@", "error":"错误：%@",
        "profileReady":"配置已就绪", "scan":"请使用客户端扫描二维码或复制链接。", "newLink":"新链接", "copyLink":"复制链接",
        "saveQR":"保存二维码…", "progress":"安装进度", "podkopTitle":"Podkop 配置生成器", "podkopSubtitle":"VLESS 或 Hysteria 2 → sing-box outbound JSON",
        "vlessLink":"连接链接", "xhttpMode":"XHTTP 模式", "compatible":"packet-up — 兼容", "visionInfo":"Vision 使用直连 RAW/TCP 传输", "hysteriaInfo":"Hysteria 2 使用 UDP 和 Salamander",
        "generate":"生成 outbound", "copyJSON":"复制 JSON", "pasteFirst":"请粘贴链接或先安装服务器。",
        "badLink":"需要有效的 VLESS 或 Hysteria 2 链接。", "realityOnly":"需要包含 sni、pbk 和 sid 的 VLESS + REALITY 配置。",
        "profileRequired":"需要 XHTTP 或带 flow=xtls-rprx-vision 的 RAW/TCP 配置。", "podkopDone":"完成。请将 JSON 粘贴到 Podkop → Proxy → Outbound Config。",
        "jsonError":"无法生成 JSON：%@", "xhttpHelp":"XHTTP 需要内置支持 XHTTP 的 sing-box。请在 Podkop 中选择 Outbound Config。",
        "visionHelp":"Vision 通过 flow=xtls-rprx-vision 传输。请在 Podkop 中选择 Outbound Config。", "shadowTitle":"导入 Shadowrocket",
        "shadowSubtitle":"粘贴链接或扫描二维码", "copyVless":"复制链接", "shadowHelp":"使用二维码扫描器或从剪贴板导入链接",
        "noProfile":"没有配置", "noProfileHelp":"请粘贴链接或在第一个标签页创建服务器。"
    ]
]

private func tr(_ key: String, _ language: AppLanguage, _ arguments: CVarArg...) -> String {
    let value = localized[language]?[key] ?? localized[.english]?[key] ?? key
    return arguments.isEmpty ? value : String(format: value, locale: Locale(identifier: "en_US_POSIX"), arguments: arguments)
}

private struct RealitySite: Identifiable {
    let name: String
    let domain: String
    var id: String { domain }
}

private let realitySites: [RealitySite] = [
    .init(name: "Xbox", domain: "www.xbox.com"),
    .init(name: "Microsoft", domain: "www.microsoft.com"),
    .init(name: "Mozilla", domain: "www.mozilla.org"),
    .init(name: "Google", domain: "www.google.com"),
    .init(name: "Amazon", domain: "www.amazon.com"),
    .init(name: "Meta", domain: "www.facebook.com"),
    .init(name: "NVIDIA", domain: "www.nvidia.com"),
    .init(name: "Adobe", domain: "www.adobe.com"),
    .init(name: "Intel", domain: "www.intel.com"),
    .init(name: "AMD", domain: "www.amd.com"),
    .init(name: "IBM", domain: "www.ibm.com"),
    .init(name: "Cisco", domain: "www.cisco.com"),
    .init(name: "Salesforce", domain: "www.salesforce.com"),
    .init(name: "Oracle", domain: "www.oracle.com"),
    .init(name: "Samsung", domain: "www.samsung.com"),
    .init(name: "Netflix", domain: "www.netflix.com"),
    .init(name: "Spotify", domain: "www.spotify.com"),
    .init(name: "GitHub", domain: "github.com"),
    .init(name: "Cloudflare", domain: "www.cloudflare.com"),
    .init(name: "OpenAI", domain: "openai.com"),
]

@MainActor
final class InstallerModel: ObservableObject {
    @Published var log = "Готово к подключению."
    @Published var link = ""
    @Published var isRunning = false
    @Published var status = "Введите данные нового сервера."

    func install(ip: String, login: String, password: String, site: String, masquerade: String,
                 profile: String, language: AppLanguage) {
        let ip = ip.trimmingCharacters(in: .whitespacesAndNewlines)
        let login = login.trimmingCharacters(in: .whitespacesAndNewlines)
        let parts = ip.split(separator: ".", omittingEmptySubsequences: false)
        guard parts.count == 4,
              parts.allSatisfy({ part in
                  guard let number = UInt8(part), !part.isEmpty else { return false }
                  return String(number) == part
              }) else {
            status = tr("badIP", language)
            return
        }
        guard login.range(of: "^[A-Za-z_][A-Za-z0-9._-]*$", options: .regularExpression) != nil else {
            status = tr("badLogin", language)
            return
        }
        guard !password.isEmpty else {
            status = tr("noPassword", language)
            return
        }
        if profile == "hysteria2",
           site.range(of: "^(?=.{1,253}$)[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?(?:\\.[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?)+$",
                      options: .regularExpression) == nil {
            status = tr("badDomain", language)
            return
        }
        guard let setupURL = Bundle.main.url(forResource: "setup_xray", withExtension: "py"),
              let askpassURL = Bundle.main.url(forResource: "askpass", withExtension: "sh") else {
            status = tr("missingFiles", language)
            return
        }

        isRunning = true
        link = ""
        log = tr("connecting", language, ip)
        status = tr("installing", language)

        DispatchQueue.global(qos: .userInitiated).async { [weak self] in
            guard let self else { return }
            let process = Process()
            process.executableURL = URL(fileURLWithPath: "/usr/bin/ssh")
            process.arguments = [
                "-o", "StrictHostKeyChecking=accept-new",
                "-o", "ConnectTimeout=12",
                "-o", "ServerAliveInterval=30",
                "-o", "ServerAliveCountMax=3",
                "-o", "PreferredAuthentications=password,keyboard-interactive",
                "-o", "PubkeyAuthentication=no",
                "-o", "NumberOfPasswordPrompts=1",
                "\(login)@\(ip)", "python3", "-", "--ip", ip, "--site", site,
                "--masquerade", masquerade, "--profile", profile,
            ]

            var environment = ProcessInfo.processInfo.environment
            environment["SSH_ASKPASS"] = askpassURL.path
            environment["SSH_ASKPASS_REQUIRE"] = "force"
            environment["DISPLAY"] = ":0"
            environment["XRAY_SSH_PASSWORD"] = password
            process.environment = environment

            let input = Pipe()
            let output = Pipe()
            process.standardInput = input
            process.standardOutput = output
            process.standardError = output

            do {
                let script = try Data(contentsOf: setupURL)
                try process.run()
                try input.fileHandleForWriting.write(contentsOf: script)
                try input.fileHandleForWriting.close()

                var allOutput = Data()
                while true {
                    let chunk = output.fileHandleForReading.availableData
                    if chunk.isEmpty { break }
                    allOutput.append(chunk)
                    let text = String(decoding: chunk, as: UTF8.self)
                    DispatchQueue.main.async { self.log.append(text) }
                }
                process.waitUntilExit()
                let completeText = String(decoding: allOutput, as: UTF8.self)
                let resultLink = completeText
                    .components(separatedBy: .newlines)
                    .first(where: {
                        $0.hasPrefix("vless://") || $0.hasPrefix("hysteria2://") || $0.hasPrefix("hy2://")
                    })
                DispatchQueue.main.async {
                    self.isRunning = false
                    if process.terminationStatus == 0, let resultLink {
                        self.link = resultLink
                        self.status = tr("done", language)
                    } else {
                        self.status = tr("failed", language)
                    }
                }
            } catch {
                if process.isRunning { process.terminate() }
                DispatchQueue.main.async {
                    self.isRunning = false
                    self.status = tr("launchError", language, error.localizedDescription)
                    self.log.append("\n\(tr("error", language, error.localizedDescription))\n")
                }
            }
        }
    }
}

struct ContentView: View {
    @StateObject private var model = InstallerModel()
    @State private var ip = ""
    @State private var login = "root"
    @State private var password = ""
    @State private var selectedSite = "www.xbox.com"
    @State private var hysteriaDomain = ""
    @State private var selectedProfile = "xhttp"
    @State private var profileLink = ""
    @State private var podkopMode = "packet-up"
    @State private var podkopJSON = ""
    @State private var podkopError = "Вставьте VLESS-ссылку или сначала установите сервер."
    @State private var language: AppLanguage = .russian

    private func qrCode(for value: String) -> CGImage? {
        let filter = CIFilter.qrCodeGenerator()
        filter.message = Data(value.utf8)
        filter.correctionLevel = "Q"
        guard let output = filter.outputImage else { return nil }
        let border: CGFloat = 4
        let borderedExtent = CGRect(x: 0, y: 0,
                                    width: output.extent.width + border * 2,
                                    height: output.extent.height + border * 2)
        let white = CIImage(color: .white).cropped(to: borderedExtent)
        let bordered = output.transformed(by: CGAffineTransform(translationX: border, y: border))
            .composited(over: white)
        let scaled = bordered.transformed(by: CGAffineTransform(scaleX: 8, y: 8))
        return CIContext().createCGImage(scaled, from: scaled.extent)
    }

    private func saveQR(_ image: CGImage) {
        let panel = NSSavePanel()
        panel.nameFieldStringValue = "xray-qr.png"
        panel.allowedContentTypes = [.png]
        guard panel.runModal() == .OK, let url = panel.url,
              let data = NSBitmapImageRep(cgImage: image).representation(using: .png, properties: [:]) else { return }
        try? data.write(to: url, options: .atomic)
    }

    private func copy(_ value: String) {
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(value, forType: .string)
    }

    private func generatePodkopOutbound() {
        let value = profileLink.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let components = URLComponents(string: value),
              let server = components.host,
              let port = components.port,
              let user = components.user else {
            podkopJSON = ""
            podkopError = tr("badLink", language)
            return
        }

        var query: [String: String] = [:]
        for item in components.queryItems ?? [] {
            if let itemValue = item.value { query[item.name.lowercased()] = itemValue }
        }
        if ["hysteria2", "hy2"].contains(components.scheme?.lowercased() ?? "") {
            guard !user.isEmpty,
                  let serverName = query["sni"], !serverName.isEmpty,
                  let obfsPassword = query["obfs-password"], !obfsPassword.isEmpty else {
                podkopJSON = ""
                podkopError = tr("badLink", language)
                return
            }
            let tagPart = server.split(separator: ".").first.map(String.init) ?? "server"
            let outbound: [String: Any] = [
                "type": "hysteria2", "tag": "hy2-\(tagPart)", "server": server,
                "server_port": port, "password": user,
                "obfs": ["type": query["obfs"] ?? "salamander", "password": obfsPassword],
                "tls": ["enabled": true, "server_name": serverName],
            ]
            do {
                let data = try JSONSerialization.data(withJSONObject: outbound, options: [.prettyPrinted, .sortedKeys])
                podkopJSON = String(decoding: data, as: UTF8.self)
                podkopError = tr("podkopDone", language)
            } catch {
                podkopJSON = ""
                podkopError = tr("jsonError", language, error.localizedDescription)
            }
            return
        }
        guard components.scheme?.lowercased() == "vless", UUID(uuidString: user) != nil else {
            podkopJSON = ""
            podkopError = tr("badLink", language)
            return
        }
        let uuid = user
        guard query["security"]?.lowercased() == "reality",
              let serverName = query["sni"], !serverName.isEmpty,
              let publicKey = query["pbk"], !publicKey.isEmpty,
              let shortID = query["sid"], !shortID.isEmpty else {
            podkopJSON = ""
            podkopError = tr("realityOnly", language)
            return
        }

        let transportType = query["type"]?.lowercased() ?? "tcp"
        let flow = query["flow"]?.lowercased() ?? ""
        let isXHTTP = transportType == "xhttp"
        let isVision = ["tcp", "raw"].contains(transportType) && flow == "xtls-rprx-vision"
        guard isXHTTP || isVision else {
            podkopJSON = ""
            podkopError = tr("profileRequired", language)
            return
        }

        let fingerprint = query["fp"].flatMap { $0.isEmpty ? nil : $0 } ?? "chrome"
        var outbound: [String: Any] = [
            "type": "vless",
            "server": server,
            "server_port": port,
            "uuid": uuid,
            "tls": [
                "enabled": true,
                "server_name": serverName,
                "utls": ["enabled": true, "fingerprint": fingerprint],
                "reality": ["enabled": true, "public_key": publicKey, "short_id": shortID],
            ],
        ]
        if isXHTTP {
            let path = query["path"].flatMap { $0.isEmpty ? nil : $0 } ?? "/"
            outbound["transport"] = [
                "type": "xhttp",
                "mode": podkopMode,
                "host": query["host"].flatMap { $0.isEmpty ? nil : $0 } ?? serverName,
                "path": path,
                "x_padding_bytes": "100-1000",
            ]
        } else {
            outbound["flow"] = "xtls-rprx-vision"
        }
        do {
            let data = try JSONSerialization.data(withJSONObject: outbound, options: [.prettyPrinted, .sortedKeys])
            podkopJSON = String(decoding: data, as: UTF8.self)
            podkopError = tr("podkopDone", language)
        } catch {
            podkopJSON = ""
            podkopError = tr("jsonError", language, error.localizedDescription)
        }
    }

    private var profileUsesXHTTP: Bool {
        guard let components = URLComponents(string: profileLink) else { return true }
        return components.queryItems?.first(where: { $0.name.lowercased() == "type" })?
            .value?.lowercased() == "xhttp"
    }

    private var profileUsesHysteria: Bool {
        guard let scheme = URLComponents(string: profileLink)?.scheme?.lowercased() else { return false }
        return scheme == "hysteria2" || scheme == "hy2"
    }

    var body: some View {
        TabView {
            installerView
                .tabItem { Label(tr("installTab", language), systemImage: "server.rack") }
            podkopView
                .tabItem { Label(tr("podkopTab", language), systemImage: "router") }
            shadowrocketView
                .tabItem { Label(tr("shadowTab", language), systemImage: "qrcode") }
        }
        .padding(14)
        .frame(minWidth: 760, minHeight: 590)
        .environment(\.layoutDirection, language == .persian ? .rightToLeft : .leftToRight)
        .onChange(of: model.link) { newLink in
            guard !newLink.isEmpty else { return }
            profileLink = newLink
            generatePodkopOutbound()
        }
        .onChange(of: language) { _ in
            if !model.isRunning {
                model.status = model.link.isEmpty ? tr("ready", language) : tr("done", language)
            }
            if podkopJSON.isEmpty { podkopError = tr("pasteFirst", language) }
            else { generatePodkopOutbound() }
        }
    }

    private var installerView: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(spacing: 12) {
                if let logoURL = Bundle.main.url(forResource: "xray-logo", withExtension: "png"),
                   let logo = NSImage(contentsOf: logoURL) {
                    Image(nsImage: logo)
                        .resizable()
                        .interpolation(.high)
                        .frame(width: 54, height: 54)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                }
                VStack(alignment: .leading, spacing: 3) {
                    Text(tr("title", language))
                        .font(.title2.bold())
                    Text(tr("subtitle", language,
                            selectedProfile == "xhttp" ? "XHTTP · TCP 8435" :
                            selectedProfile == "vision" ? "Vision · TCP 8435" : "Salamander · UDP 40460"))
                        .foregroundStyle(.secondary)
                }
                Spacer()
                VStack(alignment: .leading, spacing: 3) {
                    Text(tr("language", language)).font(.caption).foregroundStyle(.secondary)
                    Picker(tr("language", language), selection: $language) {
                        ForEach(AppLanguage.allCases) { item in Text(item.name).tag(item) }
                    }
                    .labelsHidden()
                    .frame(width: 140)
                }
            }

            Grid(alignment: .leading, horizontalSpacing: 12, verticalSpacing: 10) {
                GridRow {
                    Text(tr("ip", language))
                    TextField("203.0.113.10", text: $ip)
                        .textFieldStyle(.roundedBorder)
                }
                GridRow {
                    Text(tr("login", language))
                    TextField("root", text: $login)
                        .textFieldStyle(.roundedBorder)
                }
                GridRow {
                    Text(tr("password", language))
                    SecureField(tr("passwordHint", language), text: $password)
                        .textFieldStyle(.roundedBorder)
                }
                GridRow {
                    Text(tr("profile", language))
                    Picker(tr("profile", language), selection: $selectedProfile) {
                        Text(tr("xhttp", language)).tag("xhttp")
                        Text(tr("vision", language)).tag("vision")
                        Text(tr("hysteria", language)).tag("hysteria2")
                    }
                    .labelsHidden()
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .disabled(model.isRunning)
                }
                GridRow {
                    Text(tr(selectedProfile == "hysteria2" ? "domain" : "site", language))
                    if selectedProfile == "hysteria2" {
                        TextField("vpn.example.com", text: $hysteriaDomain)
                            .textFieldStyle(.roundedBorder)
                            .disabled(model.isRunning)
                    } else {
                        Picker(tr("site", language), selection: $selectedSite) {
                            ForEach(realitySites) { site in
                                Text("\(site.name) · \(site.domain)").tag(site.domain)
                            }
                        }
                        .labelsHidden()
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .disabled(model.isRunning)
                    }
                }
                if selectedProfile == "hysteria2" {
                    GridRow {
                        Text(tr("masqueradeSite", language))
                        Picker(tr("masqueradeSite", language), selection: $selectedSite) {
                            ForEach(realitySites) { site in
                                Text("\(site.name) · \(site.domain)").tag(site.domain)
                            }
                        }
                        .labelsHidden()
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .disabled(model.isRunning)
                    }
                }
            }
            .gridColumnAlignment(.leading)

            HStack(spacing: 12) {
                Button(tr("install", language)) {
                    let enteredPassword = password
                    password = ""
                    model.install(ip: ip, login: login, password: enteredPassword,
                                  site: selectedProfile == "hysteria2" ? hysteriaDomain.lowercased() : selectedSite,
                                  masquerade: selectedSite, profile: selectedProfile, language: language)
                }
                .buttonStyle(.borderedProminent)
                .disabled(model.isRunning)

                if model.isRunning { ProgressView().controlSize(.small) }
                Text(model.status)
                    .foregroundStyle(model.link.isEmpty ? Color(nsColor: .secondaryLabelColor) : Color.green)
                    .lineLimit(2)
            }

            if !model.link.isEmpty {
                HStack(alignment: .top, spacing: 18) {
                    if let qr = qrCode(for: model.link) {
                        Image(decorative: qr, scale: 1)
                            .interpolation(.none)
                            .resizable()
                            .frame(width: 190, height: 190)
                            .padding(12)
                            .background(.white)
                            .clipShape(RoundedRectangle(cornerRadius: 8))
                            .accessibilityLabel("QR-код профиля Xray")
                        VStack(alignment: .leading, spacing: 12) {
                            Text(tr("profileReady", language))
                                .font(.headline)
                            Text(tr("scan", language))
                                .foregroundStyle(.secondary)
                            TextField(tr("newLink", language), text: .constant(model.link))
                                .textFieldStyle(.roundedBorder)
                            HStack {
                                Button(tr("copyLink", language)) {
                                    copy(model.link)
                                }
                                Button(tr("saveQR", language)) { saveQR(qr) }
                            }
                        }
                        .frame(maxWidth: .infinity, alignment: .leading)
                    }
                }
            }

            Text(tr("progress", language))
                .font(.headline)
            ScrollViewReader { proxy in
                ScrollView {
                    Text(model.log)
                        .font(.system(size: 11, design: .monospaced))
                        .textSelection(.enabled)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .id("log-end")
                }
                .padding(10)
                .frame(maxWidth: .infinity, minHeight: 160, maxHeight: .infinity)
                .background(Color(nsColor: .textBackgroundColor))
                .clipShape(RoundedRectangle(cornerRadius: 8))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(nsColor: .separatorColor)))
                .onChange(of: model.log) { _ in
                    proxy.scrollTo("log-end", anchor: .bottom)
                }
            }
        }
        .padding(18)
    }

    private var podkopView: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack(spacing: 12) {
                Image(systemName: "router.fill")
                    .font(.system(size: 34))
                    .foregroundStyle(.blue)
                VStack(alignment: .leading, spacing: 3) {
                    Text(tr("podkopTitle", language)).font(.title2.bold())
                    Text(tr("podkopSubtitle", language))
                        .foregroundStyle(.secondary)
                }
            }

            Text(tr("vlessLink", language))
                .font(.headline)
            TextEditor(text: $profileLink)
                .font(.system(size: 11, design: .monospaced))
                .frame(height: 82)
                .padding(6)
                .background(Color(nsColor: .textBackgroundColor))
                .clipShape(RoundedRectangle(cornerRadius: 8))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(nsColor: .separatorColor)))

            HStack {
                if profileUsesHysteria {
                    Label(tr("hysteriaInfo", language), systemImage: "wave.3.right")
                        .foregroundStyle(.secondary)
                } else if profileUsesXHTTP {
                    Text(tr("xhttpMode", language))
                    Picker(tr("xhttpMode", language), selection: $podkopMode) {
                        Text(tr("compatible", language)).tag("packet-up")
                        Text("stream-up").tag("stream-up")
                        Text("stream-one").tag("stream-one")
                        Text("auto").tag("auto")
                    }
                    .labelsHidden()
                    .onChange(of: podkopMode) { _ in generatePodkopOutbound() }
                } else {
                    Label(tr("visionInfo", language), systemImage: "bolt.fill")
                        .foregroundStyle(.secondary)
                }
                Spacer()
                Button(tr("generate", language)) { generatePodkopOutbound() }
                    .buttonStyle(.borderedProminent)
            }

            HStack {
                Text("Outbound Config").font(.headline)
                Spacer()
                Button(tr("copyJSON", language)) { copy(podkopJSON) }
                    .disabled(podkopJSON.isEmpty)
            }
            TextEditor(text: .constant(podkopJSON))
                .font(.system(size: 11, design: .monospaced))
                .frame(maxHeight: .infinity)
                .padding(6)
                .background(Color(nsColor: .textBackgroundColor))
                .clipShape(RoundedRectangle(cornerRadius: 8))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(nsColor: .separatorColor)))
            Text(podkopError)
                .foregroundStyle(podkopJSON.isEmpty ? Color(nsColor: .secondaryLabelColor) : Color.green)
            Label(profileUsesHysteria
                  ? tr("hysteriaInfo", language)
                  : profileUsesXHTTP ? tr("xhttpHelp", language) : tr("visionHelp", language),
                  systemImage: "info.circle")
                .font(.callout)
                .foregroundStyle(.secondary)
        }
        .padding(18)
    }

    private var shadowrocketView: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(spacing: 12) {
                Image(systemName: "qrcode")
                    .font(.system(size: 34))
                    .foregroundStyle(.blue)
                VStack(alignment: .leading, spacing: 3) {
                    Text(tr("shadowTitle", language)).font(.title2.bold())
                    Text(tr("shadowSubtitle", language))
                        .foregroundStyle(.secondary)
                }
            }

            TextEditor(text: $profileLink)
                .font(.system(size: 11, design: .monospaced))
                .frame(height: 96)
                .padding(6)
                .background(Color(nsColor: .textBackgroundColor))
                .clipShape(RoundedRectangle(cornerRadius: 8))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(nsColor: .separatorColor)))

            HStack {
                Button(tr("copyVless", language)) { copy(profileLink) }
                    .buttonStyle(.borderedProminent)
                    .disabled(!(profileLink.hasPrefix("vless://") || profileLink.hasPrefix("hysteria2://") || profileLink.hasPrefix("hy2://")))
                Text(tr("shadowHelp", language))
                    .foregroundStyle(.secondary)
            }

            Spacer()
            if (profileLink.hasPrefix("vless://") || profileLink.hasPrefix("hysteria2://") || profileLink.hasPrefix("hy2://")),
               let qr = qrCode(for: profileLink) {
                HStack {
                    Spacer()
                    VStack(spacing: 10) {
                        Image(decorative: qr, scale: 1)
                            .interpolation(.none)
                            .resizable()
                            .frame(width: 260, height: 260)
                            .padding(14)
                            .background(.white)
                            .clipShape(RoundedRectangle(cornerRadius: 10))
                            .accessibilityLabel("QR-код для Shadowrocket")
                        Button(tr("saveQR", language)) { saveQR(qr) }
                    }
                    Spacer()
                }
            } else {
                VStack(spacing: 10) {
                    Image(systemName: "link.badge.plus")
                        .font(.system(size: 34))
                        .foregroundStyle(.secondary)
                    Text(tr("noProfile", language)).font(.headline)
                    Text(tr("noProfileHelp", language))
                        .foregroundStyle(.secondary)
                }
                .frame(maxWidth: .infinity)
            }
            Spacer()
        }
        .padding(18)
    }
}

@main
struct XrayInstallerApp: App {
    init() {
        if let url = Bundle.main.url(forResource: "xray-logo", withExtension: "png"),
           let image = NSImage(contentsOf: url) {
            NSApplication.shared.applicationIconImage = image
        }
    }
    var body: some Scene {
        WindowGroup("Xray Installer") {
            ContentView()
        }
        .windowResizability(.contentSize)
    }
}
