using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using ResultOf;

namespace FileSenderRailway
{
    public interface ICryptographer
    {
        byte[] Sign(byte[] content, X509Certificate certificate);
    }

    public interface IRecognizer
    {
        /// <exception cref="FormatException">Not recognized</exception>
        Result<Document> Recognize(FileContent file);
    }

    public interface IFileSender
    {
        Document PrepareFileToSend(FileContent file, X509Certificate certificate);
        IEnumerable<FileSendResult> SendFiles(FileContent[] files, X509Certificate certificate);
    }

    public interface ISender
    {
        /// <exception cref="InvalidOperationException">Can't send</exception>
        void Send(Document document);
    }

    public struct Document
    {
        public Document(string name, byte[] content, DateTime created, string format)
        {
            Name = name;
            Created = created;
            Format = format;
            Content = content;
        }

        public Document WithContent(byte[] content)
        {
            return new Document(Name, content, Created, Format);
        }

        public string Name { get; }
        public DateTime Created { get; }
        public string Format { get; }
        public byte[] Content { get; }
    }

    public class FileContent
    {
        public FileContent(string name, byte[] content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }
        public byte[] Content { get; }
    }

    public class FileSendResult
    {
        public FileSendResult(FileContent file, string error = null)
        {
            File = file;
            Error = error;
        }

        public FileContent File { get; }
        public string Error { get; }
        public bool IsSuccess => Error == null;
    }
}