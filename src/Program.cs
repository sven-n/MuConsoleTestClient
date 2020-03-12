// <copyright file="Program.cs" company="sven-n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MuConsoleTestClient
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using MUnique.OpenMU.Network;
    using MUnique.OpenMU.Network.SimpleModulus;
    using MUnique.OpenMU.Network.Xor;
    using Pipelines.Sockets.Unofficial;

    /// <summary>
    /// The program with the main entry point.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments. The target address can be specified as an argument in the format [IP]:[Port].</param>
        internal static async Task Main(string[] args)
        {
            var address = args.Length > 0 ? args[0] : "127.0.0.1:55901";
            var socketConnection = await SocketConnection.ConnectAsync(IPEndPoint.Parse(address));
            var encryptor = new PipelinedXor32Encryptor(new PipelinedSimpleModulusEncryptor(socketConnection.Output, PipelinedSimpleModulusEncryptor.DefaultClientKey).Writer);
            var decryptor = new PipelinedSimpleModulusDecryptor(socketConnection.Input, PipelinedSimpleModulusDecryptor.DefaultClientKey);
            var connection = new Connection(socketConnection, decryptor, encryptor);
            _ = new TestClient(connection);

            await connection.BeginReceive();

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
