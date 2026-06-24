class RcloneEncryptDeepseekCsharp < Formula
  desc "Encrypts and decrypts files using the rclone encryption defaults"
  homepage "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp"
  version "1.0.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v1.0.0/rclone-encrypt-deepseek-csharp-darwin-arm64.tar.gz"
      sha256 ""
    else
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v1.0.0/rclone-encrypt-deepseek-csharp-darwin-amd64.tar.gz"
      sha256 ""
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v1.0.0/rclone-encrypt-deepseek-csharp-linux-arm64.tar.gz"
      sha256 ""
    else
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v1.0.0/rclone-encrypt-deepseek-csharp-linux-amd64.tar.gz"
      sha256 ""
    end
  end

  def install
    bin.install "rclone-encrypt-deepseek-csharp-darwin-arm64" => "rclone-encrypt-deepseek-csharp" if OS.mac? && Hardware::CPU.arm?
    bin.install "rclone-encrypt-deepseek-csharp-darwin-amd64" => "rclone-encrypt-deepseek-csharp" if OS.mac? && !Hardware::CPU.arm?
    bin.install "rclone-encrypt-deepseek-csharp-linux-arm64" => "rclone-encrypt-deepseek-csharp" if OS.linux? && Hardware::CPU.arm?
    bin.install "rclone-encrypt-deepseek-csharp-linux-amd64" => "rclone-encrypt-deepseek-csharp" if OS.linux? && !Hardware::CPU.arm?
  end

  test do
    assert_match "rclone-encrypt-deepseek-csharp", shell_output("#{bin}/rclone-encrypt-deepseek-csharp --help")
  end
end
