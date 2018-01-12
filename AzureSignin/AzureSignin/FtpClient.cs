﻿using System;
using System.IO;
using System.Net;

namespace AzureSignin.Communication
{
    public sealed class FtpClient
    {
        private string _url = null;
        private string _username = null;
        private string _password = null;

        public FtpClient(string url, string userName, string password)
        {
            _url = url;
            _username = userName;
            _password = password;
        }

        public void UploadDirectory(string remotePath, string localPath)
        {
            string[] files = Directory.GetFiles(localPath, "*.*");
            string[] subDirs = Directory.GetDirectories(localPath);

            foreach (string file in files)
            {
                UploadFile(remotePath + "/" + Path.GetFileName(file), file);
            }

            foreach (string subDir in subDirs)
            {
                CreateDirectory(remotePath + "/" + Path.GetFileName(subDir));
                UploadDirectory(remotePath + "/" + Path.GetFileName(subDir), subDir);
            }
        }

        public void UploadFile(string remoteFile, string localFile)
        {
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(_username, _password);
                client.UploadFile(remoteFile, localFile);
            }
        }

        public void CreateDirectory(string newDirectory)
        {
            // Use FtpWebRequest since WebClient doesn't support creating directory
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(newDirectory);
            ftpRequest.Credentials = new NetworkCredential(_username, _password);
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }
    }
}
