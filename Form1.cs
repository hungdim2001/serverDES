using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TripleDES_File_Encryption_Tutorial
{
    public partial class Form1 : Form

		
    {
		IPEndPoint IP;
		Socket server;
		Socket client;
		String path;
		String fileName;
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
		
		private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                TripleDES tDES = new TripleDES(textBox2.Text);
                tDES.EncryptFile(textBox1.Text);
                GC.Collect();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                TripleDES tDES = new TripleDES(textBox2.Text);
                tDES.DecryptFile(textBox1.Text);
                GC.Collect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
			if (textBox2.Text.Length!=8)
			{
				MessageBox.Show("key must be 8 characterst");
				return;
			}
			try
			{
				FileInfo file = new FileInfo(path + fileName);
				byte[] data = new byte[1024];
				byte[] fsize = new byte[file.Length]; // tạo mảng chứa dữ liệu
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

	}
}
