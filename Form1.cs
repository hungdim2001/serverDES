using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Server.Doi_tuong;
using Server.Thu_Vien;
using System.Security.Cryptography;

namespace Server
{
    public partial class Form1 : Form

		
    {
		IPEndPoint IP;
		Socket server;
		Socket client;
		String path;
		String fileName;
		public static string TenTienTrinh = "";
		public static int GiaiDoan = -1;
		private static int Dem = 0;
		int MaHoaHayGiaiMa = 1;
		bool FileHayChuoi = true;
		DES64Bit MaHoaDES64;
		Khoa Khoa;
	
		public Form1()
        {
			Connect();
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog OD = new OpenFileDialog();
            OD.Filter = "All Files|*";
            OD.FileName = "";
			if (OD.ShowDialog()==DialogResult.OK)
            {
				path = "";
				fileName = OD.FileName.Replace("\\", "/");
				while (fileName.IndexOf("/") > -1)
				{
					path += fileName.Substring(0, fileName.IndexOf("/") + 1);
					fileName = fileName.Substring(fileName.IndexOf("/") + 1);
				}

				textBox1.Text = path+fileName;
            }
			

		}
		void Connect()
		{
			IP = new IPEndPoint(IPAddress.Any, 9999);
			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			server.Bind(IP);

			Thread listen = new Thread(() =>
			{
				try
				{
					while (true)
					{
						server.Listen(100);
						client = server.Accept();
						Thread receive = new Thread(Receive);
						receive.IsBackground = true;
						receive.Start(client);
					}
				}
				catch
				{
					IP = new IPEndPoint(IPAddress.Any, 9999);
					server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
				}
			}
				);
			listen.IsBackground = true;
			listen.Start();

		}
		void Receive(object obj)
		{
			Socket client = obj as Socket;
			try
			{
				int size;
				while (true)
				{
					byte[] data = new byte[1024];
					size = client.Receive(data);
					string[] s = Encoding.UTF8.GetString(data, 0, size).Split(new char[] { ',' }); // nhan ten file, duong dan, size.
					long length = long.Parse(s[1]);
					byte[] buffer = new byte[1024];
					byte[] fsize = new byte[length]; //khai bao mang byte de chua du lieu
					long n = length / buffer.Length;  // tính số frame sẽ được gửi qua
					for (int i = 0; i < n; i++)
					{
						size = client.Receive(fsize, fsize.Length, SocketFlags.None);
						Console.WriteLine("Received frame {0}/{1}", i, n);
					}
					FileStream fs = new FileStream("C:/Users/hungdz/Desktop/server" + "/" + s[0], FileMode.Create);  // luu file s[0] vao duong dan s[1]
					fs.Write(fsize, 0, fsize.Length);
					fs.Close();
					MessageBox.Show("receive file successfully");
					 break;
				}
			}
			catch (Exception e)
			{
				client.Close();
				//MessageBox.Show(e.Message);
			}

		}
		private void MaHoa()
		{
			
			MaHoaDES64 = new DES64Bit();

			TenTienTrinh = "";

			GiaiDoan = 0;
			Dem = 0;

			if (FileHayChuoi)
			{
				Khoa = new Khoa(textBox2.Text);
				if (MaHoaHayGiaiMa == 1)
				{
						
					GiaiDoan = 0;																	if(GiaiDoan== 0) { Encrypt(); return; }
					ChuoiNhiPhan chuoi = DocFileTxt.FileReadToBinary(fileName + path);
					GiaiDoan = 1;
					ChuoiNhiPhan KQ = MaHoaDES64.ThucHienDES(Khoa, chuoi, 1);
					GiaiDoan = 2;
					DocFileTxt.WriteBinaryToFile(fileName+path, KQ);
					GiaiDoan = 3;
					
				}
				else
				{
					GiaiDoan = 0;																		if(GiaiDoan== 0) { Decrypt(); return; }
					ChuoiNhiPhan chuoi = DocFileTxt.FileReadToBinary(fileName + path);
					GiaiDoan = 1;
					ChuoiNhiPhan KQ = MaHoaDES64.ThucHienDES(Khoa, chuoi, -1);
					if (KQ == null)
					{
						MessageBox.Show("Invalid Key");
						return;
					}
					GiaiDoan = 2;
					DocFileTxt.WriteBinaryToFile(fileName + path, KQ);
					GiaiDoan = 3;
				}
			}

		
		}

		private void button2_Click(object sender, EventArgs e)
        {
		
        }

        private void button3_Click(object sender, EventArgs e)
        {
			if (String.IsNullOrEmpty(textBox2.Text))
			{
				MessageBox.Show("key is not empty");
				return;
			}
			if (textBox2.Text.Length != 8)
			{
				MessageBox.Show("key must be 8 characterst");
				return;
			}
			MaHoaHayGiaiMa = 2;
			MaHoa();
        }

		private void button4_Click(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(textBox1.Text))
			{
				MessageBox.Show("please select file");
				return;
			}
			if (String.IsNullOrEmpty(textBox2.Text))
			{
				MessageBox.Show("key is not empty");
				return;
			}
			if (textBox2.Text.Length != 8)
			{
				MessageBox.Show("key must be 8 characterst");
				return;
			}
			try
			{
				MaHoaHayGiaiMa = 1;
				MaHoa();
				FileInfo file = new FileInfo(path + fileName);
				byte[] data = new byte[1024];
				byte[] fsize = new byte[file.Length];
				FileStream fs = new FileStream(path + fileName, FileMode.Open); // đọc thông tin file đã nhập
				fs.Read(fsize, 0, fsize.Length);
				fs.Close();
				while (true)
				{
					client.Send(Encoding.UTF8.GetBytes(fileName + "," + file.Length.ToString()));
					long n = file.Length / data.Length;  //tính số frame phải gửi

					for (int i = 0; i < n; i++)
					{
						Console.WriteLine("Sending frame {0}/{1}", i, n);
						client.Send(fsize, fsize.Length, 0);
					}
					MessageBox.Show("Send File Successfully");
					break;
				}
			}
			catch
			{
				client.Close();

			}

			
		}






















		public void Encrypt()
		{
			try
			{
				DES tDES = new DES(textBox2.Text);
				tDES.EncryptFile(textBox1.Text);
				GC.Collect();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return;
			}
		}
		public void Decrypt()
		{

			if (String.IsNullOrEmpty(textBox2.Text))
			{
				MessageBox.Show("key is not empty");
				return;
			}
			if (textBox2.Text.Length != 8)
			{
				MessageBox.Show("key must be 8 characterst");
				return;
			}
			try
			{
				DES tDES = new DES(textBox2.Text);
				tDES.DecryptFile(textBox1.Text);
				GC.Collect();
				MessageBox.Show("Decrypt successfully");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			
		}

	}
}
