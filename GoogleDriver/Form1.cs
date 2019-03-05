using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;


namespace GoogleDriver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            CheckSignIn();

        }
        //quyền truy xuất dữ liệu với Scope = Drive
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "Application_upload_file_GGD";
        public string path_Json = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "client_secret.json");
        DriveService service;
        struct FoderDaTao
            {
           public string Tenfoder;
          public  string idFoder;
            };
        List<FoderDaTao> ListFoderCreated =new List<FoderDaTao>();

        //Không xài tới nên khỏi nói
        //list danh sách file đã up
        private void ListFiles(DriveService service, ref string pageToken)
        {
            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 1;
            listRequest.Fields = "nextPageToken, files(name)";
            listRequest.PageToken = pageToken;
            listRequest.Q = "mimeType='*/*'";

            // List files.
            var request = listRequest.Execute();


            if (request.Files != null && request.Files.Count > 0)
            {


                foreach (var file in request.Files)
                {
                   // textBox1.Text += string.Format("{0}\n", file.Name);
                }

                pageToken = request.NextPageToken;

                if (request.NextPageToken != null)
                {

                    Console.ReadLine();

                }

            }
            else
            {
                //textBox1.Text += ("No files found.");
            }
        }



        
        private void UploadFile(string path, DriveService service, string folderUpload)
        {
         

            var fileMetadata = new Google.Apis.Drive.v3.Data.File();
            fileMetadata.Name = Path.GetFileName(path);
            //fileMetadata.MimeType = "image/*";
            fileMetadata.MimeType = "*/*";

            fileMetadata.Parents = new List<string>
            {
                folderUpload
            };


            FilesResource.CreateMediaUpload request;
            using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "*/*");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;

            //textBox1.Text += ("File ID: " + file.Id);

        }


        private  UserCredential GetCredentials()
        {
            UserCredential credential;
            // Thông tin về quyền truy xuất dữ liệu của người dùng được lưu ở thư mục client_secret.json
            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                //tạo foder trên máy tính về quyền truy xuất dữ liệu tại thư mục Document
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

                credPath = Path.Combine(credPath, "client_secret.json");
                // Yêu cầu người dùng xác thực lần đầu và thông tin sẽ được lưu vào thư mục client_secret.json
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,// Quyền truy xuất dữ liệu của người dùng
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                 textBox1.Text = string.Format("Credential file saved to: " + credPath);
                path_Json = credPath;
            }

            return credential;
        }
        //Xóa file trong thư mục client_secret.json
        public void EmptyFolder(DirectoryInfo directoryInfo)
        {
            try
            {
                foreach (FileInfo file in directoryInfo.GetFiles())
                {

                    file.Delete();
                }
                //foreach (DirectoryInfo subfolder in directoryInfo.GetDirectories())
                //{
                //    EmptyFolder(subfolder);
                //}
            }
            catch { }
        }

        
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            UserCredential credential;

            credential = GetCredentials();

            // Tạo ra 1 dịch vụ Drive API - Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            string folderid;
            //Tạo foder trên drive
            var fileMetadatas = new Google.Apis.Drive.v3.Data.File()
            {
                Name = txtFolderNameUpload.Text,
                MimeType = "application/vnd.google-apps.folder"
            };

            string et=fileMetadatas.Id;
            var requests = service.Files.Create(fileMetadatas);
            // Cấu hình thông tin lấy về là ID
            requests.Fields = "id";
            var files = requests.Execute();
            folderid = files.Id;

            #region phần làm thêm không cần quan tâm
            FoderDaTao a = new FoderDaTao();
            a.idFoder = folderid;
            a.Tenfoder = txtFolderNameUpload.Text;
            ListFoderCreated.Add(a);
           // MessageBox.Show("Folder ID: " + files.Id);
            foreach (FoderDaTao c in ListFoderCreated)
                textBox1.Text += string.Format("Tên {0} : ID {1}\n", c.Tenfoder, c.idFoder);
            #endregion

            //mở Dialog và chọn File cần up

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileSelected.Text = "Đang upload file đến folder: => " + txtFolderNameUpload.Text +"-Id:"+ folderid+ "\r\n\r\n";
               
                Thread thread; 
                foreach (string filename in openFileDialog1.FileNames)
                {
                    thread = new Thread(() =>
                    {
                        try
                        {
                            txtFileSelected.Text += filename;
                            //Up file
                            UploadFile(filename, service, folderid);
                            txtFileSelected.Text += " => Upload thành công..." + "\r\n";
                        }
                        catch
                        {
                            txtFileSelected.Text += " => Upload lỗi thành công..." + "\r\n";
                        }
                       
                    });
                    thread.IsBackground = true;
                    thread.Start(); 

                }
                

            }


            //string pageToken = null;

            //do
            //{
            //    ListFiles(service, ref pageToken);

            //} while (pageToken != null);

            //textBox1.Text += "Upload file thành công.";

        }
        //Đăng xuất thông tin bằng cách xóa file trong thư mục client_secret.json
        private void btnLogout_Click(object sender, EventArgs e)
        {   
            System.IO.DirectoryInfo fi = new System.IO.DirectoryInfo(path_Json);
            EmptyFolder(fi);
            path_Json = null;
            CheckSignIn();
            txtFileSelected.Text = "";
            MessageBox.Show("Bạn đã đăng xuất thành công!");
            txtFolderNameUpload.Text = "";
            ListFoderCreated.Clear();

        }
       
        public void CheckSignIn()
        {
            //kiểm tra thông tin đăng nhập đã tồn tại hay chưa
            if (System.IO.File.Exists(path_Json+ @"\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user"))
            {
                lbSignin.Text = "Bạn đã đăng nhập";
                btnBrowse.Visible = true;
                btnLogout.Visible = true;
                btnLogin.Visible = false;
            }
            else
            {
                lbSignin.Text = "Bạn chưa đăng nhập";
                btnBrowse.Visible = false;
                btnLogout.Visible = false;
                btnLogin.Visible = true;
            }

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {

            // Tạo ra 1 dịch vụ Drive API - Create Drive API service.
            try
            {
                Thread thread = new Thread(() =>
                {
                    
                        UserCredential credential;

                        credential = GetCredentials();
                        var service = new DriveService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = ApplicationName,
                        });
                        MessageBox.Show("Bạn đã đăng nhập thành công!");
                        CheckSignIn();
                    this.TopMost = true;
                   

                });
                thread.IsBackground = true;
                thread.Start();
               
                CheckSignIn();
            }
            catch(Exception ex)
            {
               MessageBox.Show("Lỗi", "Lỗi: "+ex.Message );
            }
            btnBrowse.Visible = true;
            btnLogout.Visible = true;
            btnLogin.Visible = false;
            this.TopMost = false;
            CheckSignIn();
          
        }

        private UserCredential NewMethod()
        {
            return GetCredentials();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtFileSelected.Text = "";
            txtFolderNameUpload.Text = "";
        }
    } 
}
