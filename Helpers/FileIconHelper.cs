using Avalonia.Media;
using PhosphorIconsAvalonia;

namespace sy_ftp.Helpers;

public static class FileIconHelper
{
    private static (string Icon, string Color) ForFile(string fileName)
    {
        var baseName = Path.GetFileName(fileName);
        var lower = baseName.ToLowerInvariant();

        if (TryMatchByFullName(lower, out var m)) return m;

        if (lower.EndsWith(".d.ts")) return ("file_ts", "#8A8A8A");

        var ext = Path.GetExtension(lower);
        return ext switch
        {
            // ── Plain text / logs / docs ────────────────────────────────
            ".txt"                          => ("file_txt",     "#78909C"),
            ".log"                          => ("file_text",    "#546E7A"),
            ".md" or ".markdown"            => ("file_md",      "#1565C0"),
            ".pdf"                          => ("file_pdf",     "#E53935"),
            ".rst" or ".adoc" or ".asciidoc"=> ("file_text",    "#2E7D32"),
            ".epub" or ".mobi" or ".azw"
                or ".azw3" or ".djvu"       => ("file_text",    "#85829B"),
            ".tex" or ".bib"                => ("file_text",    "#3D6117"),

            // ── Python ──────────────────────────────────────────────────
            ".py" or ".pyw" or ".pyx"
                or ".pyi"                   => ("file_py",      "#3572A5"),
            ".ipynb"                        => ("file_py",      "#F37626"),

            // ── JS / TS family ──────────────────────────────────────────
            ".js" or ".mjs" or ".cjs"       => ("file_js",      "#F0A500"),
            ".ts"                           => ("file_ts",      "#3178C6"),
            ".jsx"                          => ("file_jsx",     "#00B4D8"),
            ".tsx"                          => ("file_tsx",     "#3178C6"),
            ".vue"                          => ("file_vue",     "#42B883"),
            ".map"                          => ("file_code",    "#9E9E9E"),

            // ── Web styles & markup ─────────────────────────────────────
            ".css" or ".scss" or ".sass"
                or ".less" or ".styl"       => ("file_css",     "#264DE4"),
            ".html" or ".htm" or ".xhtml"   => ("file_html",    "#E34C26"),

            // ── C / C++ / C# / Rust ─────────────────────────────────────
            ".c" or ".h"                    => ("file_c",       "#5C6BC0"),
            ".cpp" or ".cc" or ".cxx"
                or ".hpp" or ".hh"
                or ".hxx"                   => ("file_cpp",     "#00599C"),
            ".cs" or ".csx"                 => ("file_c_sharp", "#9B4F96"),
            ".rs"                           => ("file_rs",      "#CE422B"),

            // ── JVM ─────────────────────────────────────────────────────
            ".java"                         => ("file_code",    "#EA2D2E"),
            ".kt" or ".kts"                 => ("file_code",    "#7F52FF"),
            ".scala" or ".sbt"              => ("file_code",    "#DC322F"),
            ".groovy" or ".gradle"          => ("file_code",    "#4298B8"),
            ".class" or ".jar" or ".war"
                or ".ear"                   => ("file_archive", "#EA2D2E"),

            // ── Backend / scripting languages ───────────────────────────
            ".go"                           => ("file_code",    "#00ADD8"),
            ".rb" or ".erb" or ".rbw"       => ("file_code",    "#CC342D"),
            ".php" or ".phtml" or ".phar"   => ("file_code",    "#777BB4"),
            ".lua"                          => ("file_code",    "#000080"),
            ".dart"                         => ("file_code",    "#00B4AB"),
            ".ex" or ".exs"                 => ("file_code",    "#6E4A7E"),
            ".erl" or ".hrl"                => ("file_code",    "#A90533"),
            ".clj" or ".cljs" or ".cljc"
                or ".edn"                   => ("file_code",    "#5881D8"),
            ".hs" or ".lhs"                 => ("file_code",    "#5E5086"),
            ".swift"                        => ("file_code",    "#F05138"),
            ".m" or ".mm"                   => ("file_code",    "#438EFF"),
            ".r" or ".rmd"                  => ("file_code",    "#276DC3"),
            ".pl" or ".pm"                  => ("file_code",    "#0298C3"),
            ".zig"                          => ("file_code",    "#F7A41D"),
            ".nim" or ".nims"               => ("file_code",    "#FFE953"),
            ".cr"                           => ("file_code",    "#2A2A2A"),
            ".ml" or ".mli"                 => ("file_code",    "#3BE133"),
            ".jl"                           => ("file_code",    "#9558B2"),
            ".v"                            => ("file_code",    "#5D87BF"),
            ".sol"                          => ("file_code",    "#363636"),

            // ── Shell / Windows scripts ─────────────────────────────────
            ".sh" or ".bash" or ".zsh"
                or ".fish"                  => ("file_code",    "#4EAA25"),
            ".ps1" or ".psm1" or ".psd1"    => ("file_code",    "#012456"),
            ".bat" or ".cmd"                => ("file_code",    "#8BC34A"),
            ".vbs"                          => ("file_code",    "#8A2BE2"),
            ".awk" or ".sed"                => ("file_code",    "#4EAA25"),

            // ── SQL / data / serialization ──────────────────────────────
            ".sql"                          => ("file_sql",     "#E38C00"),
            ".proto"                        => ("file_code",    "#4285F4"),
            ".graphql" or ".gql"            => ("file_code",    "#E10098"),
            ".parquet" or ".avro"
                or ".orc" or ".arrow"
                or ".feather"               => ("file_code",    "#FF7F50"),
            ".db" or ".sqlite"
                or ".sqlite3" or ".mdb"     => ("file",         "#0298C3"),

            // ── Config / markup ─────────────────────────────────────────
            ".ini" or ".cfg" or ".conf"
                or ".env" or ".toml"
                or ".properties"            => ("file_ini",     "#607D8B"),
            ".xml" or ".xsd" or ".xsl"
                or ".xslt" or ".plist"      => ("file_code",    "#607D8B"),
            ".yaml" or ".yml"               => ("file_code",    "#CB171E"),
            ".json" or ".json5"
                or ".jsonc"                 => ("file_code",    "#FBC02D"),
            ".hcl" or ".tf" or ".tfvars"    => ("file_code",    "#7B42BC"),

            // ── Archives / packages / binaries ──────────────────────────
            ".zip" or ".tar" or ".gz"
                or ".bz2" or ".xz"
                or ".7z" or ".rar"
                or ".tgz" or ".tbz2"
                or ".txz" or ".zst"
                or ".lz" or ".lzma"         => ("file_zip",     "#FF8F00"),
            ".deb" or ".rpm" or ".pkg"
                or ".dmg" or ".apk"
                or ".ipa" or ".appimage"
                or ".snap" or ".flatpak"    => ("file_archive", "#A80030"),
            ".iso" or ".img" or ".vhd"
                or ".vhdx" or ".vmdk"
                or ".qcow2"                 => ("file_archive", "#546E7A"),
            ".exe" or ".msi" or ".msu"      => ("file",         "#0078D4"),
            ".dll" or ".so" or ".dylib"
                or ".a" or ".o"
                or ".lib" or ".pdb"
                or ".obj"                   => ("file",         "#78909C"),
            ".lock"                         => ("file_lock",    "#78909C"),

            // ── Security / keys / certs ─────────────────────────────────
            ".key" or ".pem" or ".crt"
                or ".cer" or ".pub"
                or ".pfx" or ".p12"
                or ".asc" or ".gpg"
                or ".kdbx" or ".keystore"   => ("file_lock",    "#D4AF37"),

            // ── Fonts ───────────────────────────────────────────────────
            ".ttf" or ".otf" or ".woff"
                or ".woff2" or ".eot"       => ("file_text",    "#D81B60"),

            // ── Images ──────────────────────────────────────────────────
            ".jpg" or ".jpeg"               => ("file_jpg",     "#43A047"),
            ".png"                          => ("file_png",     "#43A047"),
            ".svg"                          => ("file_svg",     "#FF9800"),
            ".gif" or ".webp" or ".bmp"
                or ".ico" or ".tiff"
                or ".tif" or ".heic"
                or ".avif" or ".raw"        => ("file_image",   "#43A047"),

            // ── Design files (Adobe / Figma / Sketch) ───────────────────
            ".psd" or ".ai" or ".xd"
                or ".fig" or ".sketch"
                or ".afphoto" or ".afdesign"=> ("file_image",   "#31A8FF"),

            // ── 3D / CAD ────────────────────────────────────────────────
            ".fbx" or ".stl"
                or ".blend" or ".glb"
                or ".gltf" or ".dae"
                or ".3ds" or ".ply"         => ("file",         "#F5792A"),
            ".dwg" or ".dxf" or ".step"
                or ".stp" or ".iges"        => ("file",         "#E51B24"),

            // ── Audio / video ───────────────────────────────────────────
            ".mp3" or ".wav" or ".flac"
                or ".aac" or ".ogg"
                or ".m4a" or ".opus"
                or ".wma" or ".mid"
                or ".midi"                  => ("file_audio",   "#9C27B0"),
            ".mp4" or ".avi" or ".mkv"
                or ".mov" or ".webm"
                or ".flv" or ".wmv"
                or ".m4v" or ".mpg"
                or ".mpeg" or ".3gp"        => ("file_video",   "#F44336"),

            // ── Office ──────────────────────────────────────────────────
            ".doc" or ".docx" or ".odt"
                or ".rtf" or ".pages"       => ("file_doc",     "#2B579A"),
            ".xls" or ".xlsx" or ".ods"
                or ".numbers"               => ("file_xls",     "#217346"),
            ".csv" or ".tsv"                => ("file_csv",     "#217346"),
            ".ppt" or ".pptx" or ".odp"     => ("file_ppt",     "#D24726"),

            // ── Generic fallback ────────────────────────────────────────
            _                               => ("file",         "#78909C"),
        };
    }

    // Matches based on the full file name (no extension or special compound names).
    // Runs before the extension switch.
    private static bool TryMatchByFullName(string lower, out (string Icon, string Color) result)
    {
        // Docker
        if (lower == "dockerfile" || lower.StartsWith("dockerfile.")
            || lower == ".dockerignore" || lower == "docker-compose.yml"
            || lower == "docker-compose.yaml" || lower == "compose.yml"
            || lower == "compose.yaml")
        { result = ("file_code", "#2496ED"); return true; }

        // Make / CMake
        if (lower is "makefile" or "gnumakefile" or "bsdmakefile"
            or "cmakelists.txt" or "meson.build" or "build.zig"
            or "wscript" or "sconstruct")
        { result = ("file_code", "#064F8C"); return true; }

        // Conventional project docs
        if (lower.StartsWith("readme") || lower.StartsWith("changelog")
            || lower.StartsWith("contributing") || lower.StartsWith("notice")
            || lower.StartsWith("authors") || lower.StartsWith("maintainers")
            || lower.StartsWith("code_of_conduct") || lower.StartsWith("security"))
        { result = ("file_md", "#1565C0"); return true; }
        if (lower.StartsWith("license") || lower.StartsWith("licence")
            || lower == "copying" || lower == "copyright")
        { result = ("file_md", "#1565C0"); return true; }

        // Git dotfiles
        if (lower is ".gitignore" or ".gitattributes" or ".gitmodules"
            or ".gitkeep" or ".gitconfig" or ".mailmap")
        { result = ("file_dashed", "#F0A500"); return true; }

        // CI / CD
        if (lower is ".travis.yml" or "appveyor.yml" or ".circleci"
            or "azure-pipelines.yml" or "jenkinsfile" or ".drone.yml"
            or "bitbucket-pipelines.yml")
        { result = ("file_code", "#2088FF"); return true; }

        // Editor / linter / formatter dotfiles
        if (lower is ".editorconfig" or ".prettierrc" or ".eslintrc"
            or ".babelrc" or ".stylelintrc" or ".prettierignore"
            or ".eslintignore" or ".npmrc" or ".yarnrc" or ".nvmrc"
            or ".tool-versions" or ".rubocop.yml" or ".flake8"
            or ".markdownlint.json" or ".swiftlint.yml")
        { result = ("file_ini", "#7C4DFF"); return true; }

        // npm — package.json is the signature entry point
        if (lower == "package.json")
        { result = ("file_js", "#CB3837"); return true; }

        // Lock files (dependency pinning)
        if (lower is "package-lock.json" or "yarn.lock" or "pnpm-lock.yaml"
            or "cargo.lock" or "gemfile.lock" or "composer.lock"
            or "poetry.lock" or "pipfile.lock" or "go.sum"
            or "flake.lock" or "mix.lock" or "bun.lockb")
        { result = ("file_lock", "#78909C"); return true; }

        // Python project metadata
        if (lower is "pyproject.toml" or "pipfile" or "requirements.txt"
            or "setup.py" or "setup.cfg" or "tox.ini" or "manifest.in"
            or "conda.yaml" or "environment.yml")
        { result = ("file_ini", "#3572A5"); return true; }

        // Go
        if (lower == "go.mod")
        { result = ("file_ini", "#00ADD8"); return true; }

        // Ruby
        if (lower is "gemfile" or "rakefile" or ".ruby-version")
        { result = ("file_code", "#CC342D"); return true; }

        // JVM build
        if (lower is "build.gradle" or "settings.gradle"
            or "build.gradle.kts" or "settings.gradle.kts"
            or "pom.xml" or "build.sbt")
        { result = ("file_code", "#4298B8"); return true; }

        // Rust
        if (lower == "cargo.toml")
        { result = ("file_ini", "#CE422B"); return true; }

        result = default;
        return false;
    }

    private static readonly Dictionary<string, Geometry?> _geomCache = new();
    private static readonly Dictionary<string, IBrush> _brushCache = new();
    private static Geometry? _folderIconCache;
    private static bool _folderIconLoaded;

    private static Geometry? GetGeometryByName(string iconName)
    {
        if (_geomCache.TryGetValue(iconName, out var cached)) return cached;
        Geometry? g = null;
        if (Enum.TryParse<Icon>(iconName, ignoreCase: false, out var icon))
        {
            try { g = IconService.CreateGeometry(icon, IconType.fill); }
            catch { /* fall through — cache null */ }
        }
        _geomCache[iconName] = g;
        return g;
    }

    public static Geometry? GetFileIcon(string fileName)
    {
        var (iconName, _) = ForFile(fileName);
        return GetGeometryByName(iconName);
    }

    public static IBrush GetFileBrush(string fileName)
    {
        var (_, hex) = ForFile(fileName);
        if (_brushCache.TryGetValue(hex, out var cached)) return cached;
        var b = SolidColorBrush.Parse(hex);
        _brushCache[hex] = b;
        return b;
    }

    public static Geometry? GetFolderIcon()
    {
        if (_folderIconLoaded) return _folderIconCache;
        try { _folderIconCache = IconService.CreateGeometry(Icon.folder, IconType.fill); }
        catch { _folderIconCache = null; }
        _folderIconLoaded = true;
        return _folderIconCache;
    }
}
