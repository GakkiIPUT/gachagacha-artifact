using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Input;
using Gacha.Services;
using Gacha.ViewModels;
using Microsoft.Win32;

namespace Gacha.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        // === UI Labels (i18n) ===
public string L_DisplayImage   => LocalizationService.Tr("ui.display_image");
public string L_SortLabel      => LocalizationService.Tr("ui.sort");
public string L_SortModeScore  => LocalizationService.Tr("ui.sortmode.score");
public string L_SortModeGame   => LocalizationService.Tr("ui.sortmode.game");
public string L_PresetLabel    => LocalizationService.Tr("ui.preset");
public string L_TargetLabel    => LocalizationService.Tr("ui.target");
public string L_Descending     => LocalizationService.Tr("ui.descending");
public string L_LanguageLabel  => LocalizationService.Tr("ui.language");
public string L_ImportGood     => LocalizationService.Tr("ui.import_good");
public string L_ExportCsv      => LocalizationService.Tr("ui.export_csv");
public string L_Settings       => LocalizationService.Tr("ui.settings");
public string L_SlotLabel      => LocalizationService.Tr("ui.slot");
public string L_SlotAll        => LocalizationService.Tr("ui.slot.all");
public string L_SlotFlower     => LocalizationService.Tr("ui.slot.flower");
public string L_SlotPlume      => LocalizationService.Tr("ui.slot.plume");
public string L_SlotSands      => LocalizationService.Tr("ui.slot.sands");
public string L_SlotGoblet     => LocalizationService.Tr("ui.slot.goblet");
public string L_SlotCirclet    => LocalizationService.Tr("ui.slot.circlet");
public string L_SetFilterLabel => LocalizationService.Tr("ui.set_filter");


// ✳ これが欠けている：セット複数選択フィルタ
private readonly HashSet<string> _setFilter = new(StringComparer.OrdinalIgnoreCase);

// ボタン表示（ローカライズ後の文言を使用）
public string SetFilterButtonLabel => _setFilter.Count > 0
    ? $"{L_SetFilterLabel}({_setFilter.Count})"
    : L_SetFilterLabel;

// 言語変更時にまとめて更新通知
void RaiseUiLabels()
{
    Raise(nameof(L_DisplayImage));
    Raise(nameof(L_SortLabel));
    Raise(nameof(L_SortModeScore));
    Raise(nameof(L_SortModeGame));
    Raise(nameof(L_PresetLabel));
    Raise(nameof(L_TargetLabel));
    Raise(nameof(L_Descending));
    Raise(nameof(L_LanguageLabel));
    Raise(nameof(L_ImportGood));
    Raise(nameof(L_ExportCsv));
    Raise(nameof(L_Settings));
    Raise(nameof(L_SlotLabel));
    Raise(nameof(L_SlotAll));
    Raise(nameof(L_SlotFlower));
    Raise(nameof(L_SlotPlume));
    Raise(nameof(L_SlotSands));
    Raise(nameof(L_SlotGoblet));
    Raise(nameof(L_SlotCirclet));
    Raise(nameof(L_SetFilterLabel));
    Raise(nameof(SetFilterButtonLabel));
}


        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            // 既定言語（ja）をロードしておく
            Gacha.Services.LocalizationService.SetLanguage(_language);
        }

        void Raise([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public ObservableCollection<ArtifactVM> Artifacts { get; } = new();

        // 表示切替（既定：画像）
        bool _isImageMode = true;
        public bool IsImageMode
        {
            get => _isImageMode;
            set
            {
                _isImageMode = value;
                Raise();
            }
        }

        // ソートモード（Score / GameLike）
        string _sortMode = "Score";
        public string SortMode
        {
            get => _sortMode;
            set
            {
                _sortMode = value;
                Raise();
                Raise(nameof(IsGameLikeSort));
                ApplySort();
            }
        }
        public bool IsGameLikeSort =>
            string.Equals(SortMode, "GameLike", StringComparison.OrdinalIgnoreCase);

        // スコアプリセット（既定：CV_ONLY）
        string _selectedPreset = "CV_ONLY";
        public string SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                Raise();
                RecalcScores();
                ApplySort();
            }
        }

        // ゲーム内風ソート対象（既定：CD、降順）
        string _gameLikeKey = "CD";
        public string GameLikeKey
        {
            get => _gameLikeKey;
            set
            {
                _gameLikeKey = value;
                Raise();
                if (IsGameLikeSort)
                    ApplySort();
            }
        }
        bool _gameLikeDesc = true;
        public bool GameLikeDesc
        {
            get => _gameLikeDesc;
            set
            {
                _gameLikeDesc = value;
                Raise();
                if (IsGameLikeSort)
                    ApplySort();
            }
        }

        // ステータス
        string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                Raise();
            }
        }
        public string SummaryText => $"Artifacts: {Artifacts.Count}";

        // === フィルタ（部位／セット） ===
        string _slotFilter = "All"; // All|flower|plume|sands|goblet|circlet
        public string SlotFilter
        {
            get => _slotFilter;
            set
            {
                _slotFilter = value;
                Raise();
                ApplyFilter();
            }
        }



        public ICommand OpenSetFilterCommand => new RelayCommand(_ => OpenSetFilter());

        // 言語（既定：日本語）
        string _language = "ja";
        public string Language
        {
            get => _language;
            set
            {
                var lang = value switch
                {
                    "ja" or "en" or "zh" => value,
                    "日本語" => "ja",
                    "English" => "en",
                    "中文" => "zh",
                    _ => _language,
                };
                if (lang == _language) return;
                if (LocalizationService.SetLanguage(lang))
                {
                    _language = lang;
                    // 各カードに「言語が変わった」ことを通知して再描画
                    foreach (var a in Artifacts) a.OnLocaleChanged();
                    RaiseUiLabels();
                    System.Windows.Data.CollectionViewSource.GetDefaultView(Artifacts)?.Refresh();
                    Raise(); // Language
                    Raise(nameof(SummaryText));
                    StatusText = $"Language: {lang}";
                }
            }
        }

        // コマンド（雛形）
        public ICommand ImportGoodCommand => new RelayCommand(_ => ImportGoodDialog());
        public ICommand ExportCsvCommand => new RelayCommand(_ => ExportCsvDialog());
        public ICommand OpenSettingsCommand =>
            new RelayCommand(_ =>
            { /* 設定画面は後日 */
            });

        // 一度だけ Filter をセット
        void EnsureViewFilterHook()
        {
            var view = CollectionViewSource.GetDefaultView(Artifacts);
            if (view.Filter == null)
                view.Filter = FilterPredicate;
        }

        bool FilterPredicate(object? o)
        {
            if (o is not ArtifactVM a)
                return false;
            // 部位
            if (
                !string.Equals(SlotFilter, "All", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(a.SlotKey, SlotFilter, StringComparison.OrdinalIgnoreCase)
            )
                return false;
            // セット（選択がある場合だけ適用）
            if (_setFilter.Count > 0 && !_setFilter.Contains(a.SetKey))
                return false;
            return true;
        }

        void ApplyFilter()
        {
            EnsureViewFilterHook();
            CollectionViewSource.GetDefaultView(Artifacts).Refresh();
            Raise(nameof(SetFilterButtonLabel));
        }

        // === GOOD 読み込み（雛形） ===
        sealed class GoodRoot
        {
            public string? format { get; set; }
            public int version { get; set; }
            public GoodArtifact[]? artifacts { get; set; }
        }

        sealed class GoodSub
        {
            public string? key { get; set; }
            public double value { get; set; }
        }

        sealed class GoodArtifact
        {
            public string setKey { get; set; } = "";
            public string slotKey { get; set; } = "";
            public int rarity { get; set; }
            public int level { get; set; }
            public string mainStatKey { get; set; } = "";
            public double mainStatValue { get; set; }
            public GoodSub[]? substats { get; set; }
            public bool @lock { get; set; }
            public bool lock_ { get; set; }
        }

        static double PercentFix(string key, double v)
        {
            switch (key)
            {
                case "critRate_":
                case "critDMG_":
                case "atk_":
                case "hp_":
                case "def_":
                case "enerRech_":
                    return v <= 1.0 ? v * 100.0 : v; // 0.039 → 3.9
            }
            return v;
        }

        (double CR, double CD, double ATKp, double HPp, double DEFp, double EM) CollectSubs(
            GoodSub[]? subs
        )
        {
            double CR = 0,
                CD = 0,
                ATKp = 0,
                HPp = 0,
                DEFp = 0,
                EM = 0;
            if (subs != null)
            {
                foreach (var s in subs)
                {
                    if (s?.key is null)
                        continue;
                    var v = PercentFix(s.key, s.value);
                    switch (s.key)
                    {
                        case "critRate_":
                            CR += v;
                            break;
                        case "critDMG_":
                            CD += v;
                            break;
                        case "atk_":
                            ATKp += v;
                            break;
                        case "hp_":
                            HPp += v;
                            break;
                        case "def_":
                            DEFp += v;
                            break;
                        case "eleMas":
                            EM += v;
                            break;
                        // ER/固定値は読み飛ばし
                    }
                }
            }
            return (CR, CD, ATKp, HPp, DEFp, EM);
        }

        string FormatMain(string key, double v)
        {
            v = PercentFix(key, v);
            return key switch
            {
                "critRate_"
                or "critDMG_"
                or "atk_"
                or "hp_"
                or "def_"
                or "enerRech_"
                or "pyro_dmg_"
                or "cryo_dmg_"
                or "dendro_dmg_"
                or "electro_dmg_"
                or "anemo_dmg_"
                or "geo_dmg_"
                or "hydro_dmg_"
                or "physical_dmg_"
                or "heal_" => $"{key}:{v:F1}%",
                "eleMas" => $"{key}:{Math.Round(v)}",
                _ => $"{key}:{Math.Round(v)}",
            };
        }

        void ImportGoodDialog()
        {
            var dlg = new OpenFileDialog
            {
                Title = "GOOD JSON を選択",
                Filter = "GOOD JSON (*.json)|*.json|すべてのファイル (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false,
            };
            if (dlg.ShowDialog() == true)
            {
                StatusText = "Importing...";
                ImportGood(dlg.FileName);
            }
        }

        public void ImportGood(string path)
        {
            if (!File.Exists(path))
            {
                StatusText = "File not found";
                return;
            }
            var json = File.ReadAllText(path);
            var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<GoodRoot>(json, opt);
            Artifacts.Clear();

            if (root?.artifacts != null)
            {
                foreach (var a in root.artifacts)
                {
                    var subs = CollectSubs(a.substats);
                    var (paths, has) = ImageProvider.Resolve(a.setKey, a.slotKey);
                    var vm = new ArtifactVM
                    {
                        SetKey = a.setKey,
                        SlotKey = a.slotKey,
                        Rarity = a.rarity,
                        Level = a.level,
                        MainText = FormatMain(a.mainStatKey, a.mainStatValue),
                        CR = Math.Round(subs.CR, 1),
                        CD = Math.Round(subs.CD, 1),
                        ATKp = Math.Round(subs.ATKp, 1),
                        HPp = Math.Round(subs.HPp, 1),
                        DEFp = Math.Round(subs.DEFp, 1),
                        EM = Math.Round(subs.EM, 1),
                        IsLocked = a.@lock || a.lock_,
                        ImagePath = paths.Path,
                        PlaceholderPath = paths.Placeholder,
                        HasImage = has,
                    };
                    vm.UpdateScore(ParsePreset(SelectedPreset));
                    Artifacts.Add(vm);
                }
            }
            Raise(nameof(SummaryText));
            ApplyFilter();
            ApplySort();
            StatusText = $"Imported: {Artifacts.Count}";
        }

        void ExportCsvDialog()
        {
            var dlg = new SaveFileDialog
            {
                Title = "CSVの保存先を選択",
                Filter = "CSVファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
                FileName = $"artifacts_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                AddExtension = true,
                OverwritePrompt = true,
            };

            if (dlg.ShowDialog() == true)
            {
                // Excel互換のためUTF-8 BOM付き
                using var sw = new StreamWriter(
                    dlg.FileName,
                    false,
                    new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
                );
                sw.WriteLine("Set,Slot,Rarity,Lv,Main,CR,CD,ATK%,HP%,DEF%,EM,Score");
                foreach (var a in Artifacts)
                {
                    // Mainは念のため二重引用符（既存仕様踏襲）
                    sw.WriteLine(
                        $"{a.SetName},{a.SlotName},{a.Rarity},{a.Level},\"{a.MainText}\",{a.CR:F1},{a.CD:F1},{a.ATKp:F1},{a.HPp:F1},{a.DEFp:F1},{a.EM:F1},{a.Score:F1}"
                    );
                }
                StatusText = $"Exported: {dlg.FileName}";
            }
            else
            {
                StatusText = "Export canceled";
            }
        }

        ScoreService.Preset ParsePreset(string s) =>
            Enum.TryParse<ScoreService.Preset>(s, out var p) ? p : ScoreService.Preset.CV_ONLY;

        void RecalcScores()
        {
            var p = ParsePreset(SelectedPreset);
            foreach (var a in Artifacts)
                a.UpdateScore(p);
        }

        void ApplySort()
        {
            EnsureViewFilterHook();
            var view = CollectionViewSource.GetDefaultView(Artifacts);
            view.SortDescriptions.Clear();

            if (!IsGameLikeSort)
            {
                view.SortDescriptions.Add(
                    new SortDescription(nameof(ArtifactVM.Score), ListSortDirection.Descending)
                );
                view.SortDescriptions.Add(
                    new SortDescription(nameof(ArtifactVM.CR), ListSortDirection.Descending)
                );
                view.SortDescriptions.Add(
                    new SortDescription(nameof(ArtifactVM.CD), ListSortDirection.Descending)
                );
            }
            else
            {
                var key = GameLikeKey switch
                {
                    "CR" => nameof(ArtifactVM.CR),
                    "CD" => nameof(ArtifactVM.CD),
                    "ATKp" => nameof(ArtifactVM.ATKp),
                    "HPp" => nameof(ArtifactVM.HPp),
                    "DEFp" => nameof(ArtifactVM.DEFp),
                    "EM" => nameof(ArtifactVM.EM),
                    _ => nameof(ArtifactVM.CD),
                };
                view.SortDescriptions.Add(
                    new SortDescription(
                        key,
                        GameLikeDesc ? ListSortDirection.Descending : ListSortDirection.Ascending
                    )
                );
            }
            view.Refresh();
        }

        // === セット絞り込みダイアログ ===
        void OpenSetFilter()
        {
            // 現在の一覧からセット候補を作成（ローカライズ名で表示）
            var options = Artifacts
                .Select(a => a.SetKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(k =>
                {
                    var nameCur = LocalizationService.TrSet(k); // 現在言語
                    var ja = LocalizationService.TrSetLang(k, "ja");
                    var en = LocalizationService.TrSetLang(k, "en");
                    var zh = LocalizationService.TrSetLang(k, "zh");
                    var tokens = $"{ja}|{en}|{zh}|{k}".ToLowerInvariant();
                    return new SetOption
                    {
                        Key = k,
                        Name = nameCur,
                        IsChecked = _setFilter.Contains(k),
                        Tokens = tokens,
                    };
                })
                .OrderBy(x => x.Name, StringComparer.CurrentCulture)
                .ToList();

            var dlg = new Gacha.Views.SetFilterDialog(options);
            var ok = dlg.ShowDialog() == true;
            if (!ok)
                return;

            _setFilter.Clear();
            foreach (var k in dlg.ResultKeys)
                _setFilter.Add(k);
            ApplyFilter();
            StatusText =
                _setFilter.Count == 0 ? "Set filter cleared" : $"Set filtered: {_setFilter.Count}";
        }
    }

    // 簡易RelayCommand
    public sealed class RelayCommand : ICommand
    {
        readonly Action<object?> _act;

        public RelayCommand(Action<object?> act) => _act = act;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _act(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
