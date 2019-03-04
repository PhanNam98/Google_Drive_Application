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
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "Application_upload_file_GGD";
        public string path_Json = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "client_secreta.json");
        DriveService service;
        struct FoderDaTao
            {
           public string Tenfoder;
          public  string idFoder;
            };
        List<FoderDaTao> ListFoderCreated =new List<FoderDaTao>();


        private void ListFiles(DriveService service, ref string pageToken)
        {
            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 1;
            //listRequest.Fields = "nextPageToken, files(id, name)";
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




        private void UploadImage(string path, DriveService service, string folderUpload)
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

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

                credPath = Path.Combine(credPath, "client_secret.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                 textBox1.Text = string.Format("Credential file saved to: " + credPath);
                path_Json = credPath;
            }

            return credential;
        }
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

            //Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            string folderid;
            //get folder id by name
            var fileMetadatas = new Google.Apis.Drive.v3.Data.File()
            {
                Name = txtFolderNameUpload.Text,
                MimeType = "application/vnd.google-apps.folder"
            };
            string et=fileMetadatas.Id;
            var requests = service.Files.Create(fileMetadatas);
            requests.Fields = "id";
            var files = requests.Execute();
            folderid = files.Id;
            FoderDaTao a = new FoderDaTao();
            a.idFoder = folderid;
            a.Tenfoder = txtFolderNameUpload.Text;
            ListFoderCreated.Add(a);
           // MessageBox.Show("Folder ID: " + files.Id);
            foreach (FoderDaTao c in ListFoderCreated)
                textBox1.Text += string.Format("Tên {0} : ID {1}\n", c.Tenfoder, c.idFoder);

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileSelected.Text = "Đang upload file đến folder: => " + txtFolderNameUpload.Text + "\r\n\r\n";
               
                Thread thread; 
                foreach (string filename in openFileDialog1.FileNames)
                {
                    thread = new Thread(() =>
                    {
                        try
                        {
                            txtFileSelected.Text += filename;
                            UploadImage(filename, service, folderid);
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


            string pageToken = null;

            do
            {
                ListFiles(service, ref pageToken);

            } while (pageToken != null);

            //textBox1.Text += "Upload file thành công.";

        }

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
            if (System.IO.File.Exists(path_Json+ "\\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user"))
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
            UserCredential credential;

            credential = GetCredentials();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            MessageBox.Show("Bạn đã đăng nhập thành công!");
            CheckSignIn();
            
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtFileSelected.Text = "";
        }
    } 
}
