// <copyright file="TestClient.cs" company="sven-n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MuConsoleTestClient
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Text;
    using MUnique.OpenMU.Network;
    using MUnique.OpenMU.Network.Packets;
    using MUnique.OpenMU.Network.Packets.ClientToServer;
    using MUnique.OpenMU.Network.Packets.ServerToClient;
    using MUnique.OpenMU.Network.Xor;

    /// <summary>
    /// A basic test client which logs into the server and selects a character.
    /// </summary>
    public class TestClient
    {
        private delegate void HandlePacket(Span<byte> packet);

        private readonly IDictionary<byte, HandlePacket> packetHandlers = new Dictionary<byte, HandlePacket>();
        private readonly IDictionary<byte, HandlePacket> characterHandlers = new Dictionary<byte, HandlePacket>();
        private readonly IConnection connection;

        /// <summary>
        /// You need a <see cref="Xor3Encryptor"/> for the username and password of the login packet.
        /// </summary>
        private readonly Xor3Encryptor xor3Encryptor = new Xor3Encryptor(0);

        /// <summary>
        /// The player identifier.
        /// </summary>
        private ushort playerId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClient"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public TestClient(IConnection connection)
        {
            this.connection = connection;
            this.connection.PacketReceived += this.OnPacketReceived;

            this.packetHandlers.Add(GameServerEntered.Code, this.HandleLoginLogout);
            this.packetHandlers.Add(CharacterList.Code, this.HandleCharacterPackets);
            this.characterHandlers.Add(CharacterList.SubCode, this.HandleCharacterList);
            this.characterHandlers.Add(CharacterInformation.SubCode, this.HandleCharacterInformation);

            this.packetHandlers.Add(AddNpcsToScope.Code, this.HandleAddNpcsToScope);
            this.packetHandlers.Add(AddCharactersToScope.Code, this.HandleAddCharactersToScope);
        }

        private void OnPacketReceived(object sender, ReadOnlySequence<byte> packetSequence)
        {
            // In this example I try to show you a pattern how you can read a packet with a minimum of memory allocations.

            // First, we rent some memory from the pool
            using var memoryOwner = MemoryPool<byte>.Shared.Rent((int)packetSequence.Length);

            // The memory we get from the pool is bigger than the sequence which we received, so we first slice it to the right size.
            var packet = memoryOwner.Memory.Slice(0, (int)packetSequence.Length).Span;

            // Then we need to copy the content of the sequence to our packet buffer
            packetSequence.CopyTo(packet);

            // now we need to check which kind of packet we received. In this example, I use some simple if statements.
            // In more complex scenarios, I suggest to use some kind of data structure which can be maintained easier.
            if (this.packetHandlers.TryGetValue(packet.GetPacketType(), out var handler))
            {
                handler(packet);
            }
        }

        private void HandleLoginLogout(Span<byte> packet)
        {
            if (packet.GetPacketSubType() == GameServerEntered.SubCode)
            {
                this.HandleGameServerEntered(packet);
            }
            else if (packet.GetPacketSubType() == LoginResponse.SubCode)
            {
                this.HandleLoginResponse(packet);
            }
            else if (packet.GetPacketSubType() == LogoutResponse.SubCode)
            {
                this.HandleLogoutResponse(packet);
            }
        }

        private void HandleCharacterList(Span<byte> packet)
        {
            CharacterList characterList = packet;
            Console.WriteLine($"Received character list, count: {characterList.CharacterCount}, Characters:");
            for (int i = 0; i < characterList.CharacterCount; i++)
            {
                var character = characterList[i];
                Console.WriteLine($"Index: {character.SlotIndex}, Name: {character.Name}, Level: {character.Level}, Status: {character.Status}");
            }

            Console.Write("Which character should be selected (please enter the name)? ");
            var name = Console.ReadLine();

            this.connection.SendSelectCharacter(name);

            Console.WriteLine("Sent selection packet");
        }

        private void HandleCharacterInformation(Span<byte> packet)
        {
            CharacterInformation characterInformation = packet;
            Console.WriteLine($"Character entered the game on map {characterInformation.MapId}, Health: {characterInformation.CurrentHealth}/{characterInformation.MaximumHealth}");
        }

        private void HandleCharacterPackets(Span<byte> packet)
        {
            if (this.characterHandlers.TryGetValue(packet.GetPacketSubType(), out var handler))
            {
                handler(packet);
            }
        }

        private void HandleLoginResponse(Span<byte> packet)
        {
            LoginResponse loginResponse = packet;
            if (loginResponse.Success == LoginResponse.LoginResult.Okay)
            {
                Console.WriteLine("Login successful");
                this.connection.SendRequestCharacterList();
                Console.WriteLine("Requested character list");
            }
            else
            {
                Console.WriteLine($"Login failed, reason: {loginResponse.Success}");
            }
        }

        private void HandleGameServerEntered(Span<byte> packet)
        {
            GameServerEntered enteredMessage = packet;
            this.playerId = enteredMessage.PlayerId;

            Console.WriteLine($"Received GameServerEntered packet, player id: {enteredMessage.PlayerId}, version: {enteredMessage.VersionString}");
            Console.Write("Enter Username: ");
            var username = Console.ReadLine();
            Console.Write("Enter Password: ");
            var password = Console.ReadLine();

            // Because this packet has some special encrypted fields, we use the "StartWrite..." extension method
            using var writer = this.connection.StartWriteLoginLongPassword();
            var loginPacket = writer.Packet;
            loginPacket.Password.WriteString(password, Encoding.ASCII);
            loginPacket.Username.WriteString(username, Encoding.ASCII);
            this.xor3Encryptor.Encrypt(loginPacket.Username);
            this.xor3Encryptor.Encrypt(loginPacket.Password);

            enteredMessage.Version.CopyTo(loginPacket.ClientVersion);

            loginPacket.TickCount = (uint)Environment.TickCount;

            writer.Commit();
            Console.WriteLine("Sent login packet");
        }

        private void HandleAddCharactersToScope(Span<byte> packet)
        {
            AddCharactersToScope charactersPacket = packet;
            for (int i = 0; i < charactersPacket.CharacterCount; i++)
            {
                var character = charactersPacket[i];

                Console.WriteLine($"Player in Scope, Id {character.Id}, Name {character.Name} {(this.playerId == character.Id ? " [Hey, that's me]" : string.Empty)}, X: {character.TargetPositionX}, Y: {character.TargetPositionY}, Rotation: {character.Rotation}");
            }
        }

        private void HandleAddNpcsToScope(Span<byte> packet)
        {
            AddNpcsToScope npcPacket = packet;
            for (int i = 0; i < npcPacket.NpcCount; i++)
            {
                var npc = npcPacket[i];
                Console.WriteLine($"NPC in Scope, Id {npc.Id}, Number {npc.TypeNumber}, X: {npc.TargetPositionX}, Y: {npc.TargetPositionY}, Rotation: {npc.Rotation}");
            }
        }

        private void HandleLogoutResponse(Span<byte> packet)
        {
            LogoutResponse logoutResponse = packet;
            Console.WriteLine($"Logout response: {logoutResponse.Type}");
        }
    }
}