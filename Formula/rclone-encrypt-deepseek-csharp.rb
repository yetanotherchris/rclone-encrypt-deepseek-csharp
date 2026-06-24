class RcloneEncryptDeepseekCsharp < Formula
  desc "Encrypts and decrypts files using the rclone encryption defaults"
  homepage "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp"
  version "0.1.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v0.1.0/rclone-encrypt-deepseek-csharp-darwin-arm64.tar.gz"
      sha256 "a39893e95248123e76309016c81564ea55110ac1d3f6042bdf572b4df8e9eb75"
    else
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v0.1.0/rclone-encrypt-deepseek-csharp-darwin-amd64.tar.gz"
      sha256 "15d300c151e277a764c331a63bbfe2b3e074e00312a2dfe4b998d1b04c999940"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v0.1.0/rclone-encrypt-deepseek-csharp-linux-arm64.tar.gz"
      sha256 "dc38028215e4d45285ba073ef322759c04ae0469d35628bad4d18b13ea20a459"
    else
      url "https://github.com/yetanotherchris/rclone-encrypt-deepseek-csharp/releases/download/v0.1.0/rclone-encrypt-deepseek-csharp-linux-amd64.tar.gz"
      sha256 "83dc862971f9820677008a46516ba0f971e0c520267bc432d1c33a006ef2b2e0"
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