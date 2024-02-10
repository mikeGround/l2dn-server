﻿using L2Dn.AuthServer.Cryptography;
using L2Dn.Cryptography;
using L2Dn.Packets;

namespace L2Dn.AuthServer.Network.Client;

internal sealed class AuthPacketEncoderFactory: IPacketEncoderFactory<AuthSession>
{
    private readonly OldBlowfishEngine _staticBlowfishEngine = new(StaticBlowfishKeys.P447());

    public IPacketEncoder Create(AuthSession session) => new AuthPacketEncoder(session.BlowfishEngine);

    // public IPacketEncoder Create(AuthSession session) =>
    //     new NewAuthPacketEncoder(_staticBlowfishEngine, session.BlowfishEngine);
}
