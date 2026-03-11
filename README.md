# Master Data Downloader

Google Spreadsheets から CSV をダウンロードし、ポストプロセッシングフックを実行する Unity Editor ツールです。

## 要件

- Unity 6 (6000.3+)
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.10+

## インストール

Unity Package Manager で以下の Git URL を追加してください:

```
https://github.com/gameshalico/master-data-downloader.git
```

または `Packages/manifest.json` に直接追記:

```json
{
  "dependencies": {
    "com.gameshalico.master-data-downloader": "https://github.com/gameshalico/master-data-downloader.git"
  }
}
```

## 使い方

### 1. SheetRegistry の作成

Project ウィンドウで右クリック > **Create > MasterDataDownloader > Sheet Registry** を選択し、アセットを作成します。

### 2. エントリの設定

作成した SheetRegistry に以下の情報を設定します:

| 項目 | 説明 |
|------|------|
| **Sheet ID** | Google Spreadsheets の URL に含まれる ID (`https://docs.google.com/spreadsheets/d/{ここ}/edit`) |
| **Sheet Name** | ダウンロード対象のシート名 |
| **Output Path** | CSV の出力先パス (例: `Assets/Data/master.csv`) |

### 3. ダウンロード

メニューから **Tools > Master Data Downloader** を開き、SheetRegistry を選択して **Download All** をクリックします。

個別エントリの **Download** ボタンで1件ずつダウンロードすることもできます。

## ポストプロセッサ

ダウンロード後に自動実行される処理を `ICsvPostProcessor` を実装して追加できます。

```csharp
public sealed class MyPostProcessor : ICsvPostProcessor
{
    public string TargetPath => "Assets/Data/master.csv";
    public string DisplayName => "Master CSV → ScriptableObject";

    public void Execute(string csvPath)
    {
        // CSV を読み込んで ScriptableObject に変換するなど
    }
}
```

実装クラスはリフレクションで自動検出されます。パラメータなしコンストラクタが必要です。

**Run All Hooks** ボタンでダウンロードなしにポストプロセッサのみ実行することもできます。

## API

### CsvDownloader

```csharp
// 単一エントリをダウンロード
await CsvDownloader.DownloadAsync(entry, cancellationToken);

// レジストリ内の全エントリをダウンロード
await CsvDownloader.DownloadAllAsync(registry, progress, cancellationToken);
```

### CsvPostProcessorRegistry

```csharp
// 特定パスのポストプロセッサを取得
IReadOnlyList<ICsvPostProcessor> processors = CsvPostProcessorRegistry.GetForPath(outputPath);

// 特定パスのポストプロセッサを実行 (戻り値は失敗数)
int failCount = CsvPostProcessorRegistry.ExecuteForPath(outputPath);
```

## ライセンス

MIT License
