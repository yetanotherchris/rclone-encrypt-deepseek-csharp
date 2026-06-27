# cli-deepseek-csharp
A small CLI tool that encrypts and decrypts using the rclone encryption defaults. Written in C# and published as a single-file, self-contained binary for Windows, macOS, and Linux.

Rclone uses a custom salt if no salt is provided, which this tool will use by default. A few similar tools:

- https://github.com/rclone/rclone
- https://github.com/mcolatosti/rclonedecrypt
- https://github.com/br0kenpixel/rclone-rcc
- @fyears/rclone-crypt

Rclone encryption uses:
- NaCl SecretBox (XSalsa20 + Poly1305) for the file contents.
- AES-256-EME for the filenames.
- scrypt (N=16384, r=8, p=1) for key derivation.

## Installation

**Homebrew (macOS/Linux)**
```bash
brew tap yetanotherchris/cli-deepseek-csharp https://github.com/yetanotherchris/cli-deepseek-csharp
brew install cli-deepseek-csharp
```

**Scoop (Windows)**
```powershell
scoop bucket add cli-deepseek-csharp https://github.com/yetanotherchris/cli-deepseek-csharp
scoop install cli-deepseek-csharp
```

## Usage

The tool detects whether a file is already encrypted (by checking for the `RCLONE` magic header) and either encrypts or decrypts it automatically.

### Basic example

```bash
# Encrypt a file (you'll be prompted for password and optional salt)
cli-deepseek-csharp -i plaintext.txt
# Written to: plaintext.txt.encrypted
# (or, if filename encryption succeeds, the encrypted filename)

# Decrypt an encrypted file
cli-deepseek-csharp -i encrypted_file.bin
# Written to: decrypted_filename.txt
```

### Using --password (not recommended)

```bash
# WARNING: --password is visible in process lists and shell history
cli-deepseek-csharp --password "mypassword" -i file.txt
```

### Using environment variable (recommended)

```bash
# Set via environment variable
$env:RCLONE_ENCRYPT_PASSWORD = "mypassword"
cli-deepseek-csharp -i file.txt
```

### With a custom salt

```bash
cli-deepseek-csharp --password "mypassword" --salt "mycustomsalt" -i file.txt
```

### Specifying output file

```bash
cli-deepseek-csharp -i encrypted.bin -o decrypted.txt
```

### Filename encoding

```bash
# Use base64 filename encoding (default is base32)
cli-deepseek-csharp --filename-encoding base64 -i file.txt
```

## Flags

| Flag | Default | Description |
|------|---------|-------------|
| `--password` | *(prompt)* | Password for encryption/decryption. Use env var `RCLONE_ENCRYPT_PASSWORD` instead |
| `--salt` | *(rclone default)* | Optional salt for key derivation |
| `--filename-encoding` | `base32` | Filename encoding: `base32` (case-insensitive compatible) or `base64` (shorter names) |
| `-i`, `--input-file` | *(required)* | Input file path |
| `-o`, `--output-file` | *(auto)* | Output file path (if omitted, derived from input) |

## Building from Source

Requires .NET 10 SDK.

```bash
git clone https://github.com/yetanotherchris/cli-deepseek-csharp
cd cli-deepseek-csharp
dotnet publish src/cli-deepseek-csharp -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

## Releases

Pushing a `vX.Y.Z` tag triggers the [Build and Release workflow](.github/workflows/build-release.yml), which cross-compiles binaries for Linux and macOS (amd64/arm64) and Windows (amd64), publishes a GitHub Release, and updates the Scoop manifest (`cli-deepseek-csharp.json`) and Homebrew formula (`Formula/cli-deepseek-csharp.rb`) in this repo.
