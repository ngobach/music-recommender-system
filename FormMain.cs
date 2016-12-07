using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting.Native;
using MyMediaLite.Data;
using MyMediaLite.DataType;
using MyMediaLite.ItemRecommendation;
using Newtonsoft.Json;

namespace DXample
{
    public partial class FormMain : RibbonForm
    {
        private readonly List<YeuThich> relas;
        private readonly List<BaiHat> songs;

        public FormMain()
        {
            InitializeComponent();
            songs = new List<BaiHat>();
            relas = new List<YeuThich>();
            gridSongs.DataSource = songs;
            gridRelas.DataSource = relas;
        }

        private void barButtonItem16_ItemClick(object sender, ItemClickEventArgs e)
        {
            XtraMessageBox.Show(this,
                "Recommender system\n" +
                "Hệ khuyến nghị bài hát phù hợp với sở thích của người dùng.\n" +
                "Phát triển bởi Ngô Xuân Bách và Đinh Viết Nam",
                "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void barButtonItem17_ItemClick(object sender, ItemClickEventArgs e)
        {
            XtraMessageBox.Show(this, "Hướng dẫn sử dụng đang được cập nhật", "Hướng dẫn", MessageBoxButtons.OK,
                MessageBoxIcon.Question);
        }

        private void ImportSongs(object sender, ItemClickEventArgs e)
        {
            var tmp = Importer<BaiHat>.Import();
            if (tmp == null) return;
            songs.AddRange(tmp);
            gridSongs.RefreshDataSource();
        }

        private void barButtonItem7_ItemClick(object sender, ItemClickEventArgs e)
        {
            var gv = (GridView) gridSongs.MainView;
            if (gv.SelectedRowsCount == 0) return;
            songs.Remove((BaiHat) gv.GetFocusedRow());
            gridSongs.RefreshDataSource();
        }

        private void barButtonItem11_ItemClick(object sender, ItemClickEventArgs e)
        {
            songs.Clear();
            gridSongs.RefreshDataSource();
        }

        private void ImportRelations(object sender, ItemClickEventArgs e)
        {
            var tmp = Importer<YeuThich>.Import();
            if (tmp == null) return;
            relas.AddRange(tmp);
            gridRelas.RefreshDataSource();
        }

        private void DeleteRelation(object sender, ItemClickEventArgs e)
        {
            var gv = (GridView) gridRelas.MainView;
            if (gv.SelectedRowsCount == 0) return;
            relas.Remove((YeuThich) gv.GetFocusedRow());
            gridRelas.RefreshDataSource();
        }

        private void DeleteAllRelations(object sender, ItemClickEventArgs e)
        {
            relas.Clear();
            gridRelas.RefreshDataSource();
        }

        private void btnOpen_ItemClick(object sender, ItemClickEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = "Dataset file(*.ds)|*.ds"};
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                using (var stream = new GZipStream(File.OpenRead(dlg.FileName), CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var ds = JsonConvert.DeserializeObject<Dataset>(reader.ReadToEnd());

                        songs.Clear();
                        songs.AddRange(ds.ListBaiHat);
                        gridSongs.RefreshDataSource();

                        relas.Clear();
                        relas.AddRange(ds.ListYeuThich);
                        gridRelas.RefreshDataSource();
                    }
                }
            }
        }

        private void btnSave_ItemClick(object sender, ItemClickEventArgs e)
        {
            var dlg = new SaveFileDialog {Filter = "Dataset file(*.ds)|*.ds"};
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                using (var stream = new GZipStream(File.OpenWrite(dlg.FileName), CompressionLevel.Optimal))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        var ds = new Dataset {ListBaiHat = songs, ListYeuThich = relas};
                        writer.Write(JsonConvert.SerializeObject(ds));
                    }
                }
            }
        }

        private void btnProcess_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Validating
            if (editUserID.EditValue.ToString().IsEmpty())
            {
                XtraMessageBox.Show(this, "Vui lòng nhập ID User", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var uid = int.Parse(editUserID.EditValue.ToString());
            var count = int.Parse(editCount.EditValue.ToString());

            if (relas.All(r => r.UserID != uid))
            {
                XtraMessageBox.Show(this, "User ID không hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ItemRecommender recommender;
            if (cbType.EditValue.ToString().Equals("User Based"))
            {
                recommender = new UserKNN();
            }
            else if (cbType.EditValue.ToString().Equals("Item Based"))
            {
                recommender = new ItemKNN();
            }
            else
            {
                XtraMessageBox.Show(this, "Invalid value " + cbType.EditValue, "Lỗi", MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk);
                return;
            }
            var mat = new PosOnlyFeedback<SparseBooleanMatrix>();
            foreach (var rela in relas)
                mat.Add(rela.UserID, rela.SongID);
            recommender.Feedback = mat;
            recommender.Train();
            var result = recommender.Recommend(uid, count);
            gridResult.DataSource =
                result.Select(i => new KetQua {BaiHat = songs.Find(song => song.ID == i.Item1).Name, DiemSo = i.Item2})
                    .ToList();
            gridResult.RefreshDataSource();
        }

        private void btnSaveResult_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (gridResult.DataSource == null || ((List<KetQua>) gridResult.DataSource).IsEmpty())
            {
                XtraMessageBox.Show(this, "Kết quả trống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var dlg = new SaveFileDialog {Filter = "Text file (*.txt)|*.txt"};
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var items = (List<KetQua>) gridResult.DataSource;
            using (var writer = new StreamWriter(File.OpenWrite(dlg.FileName)))
            {
                writer.WriteLine("Recommender system result");
                writer.WriteLine("Mode: {0}\n", cbType.EditValue);
                writer.WriteLine(new string('=', 80));
                foreach (var item in items)
                {
                    writer.WriteLine("{0} (score = {1})", item.BaiHat, item.DiemSo);
                }
                writer.WriteLine(new string('=', 80));
                writer.WriteLine("Done writing {0} result(s)", items.Count);
            }
            XtraMessageBox.Show(this, "File kết quả được ghi thành công\n" + dlg.FileName, "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}