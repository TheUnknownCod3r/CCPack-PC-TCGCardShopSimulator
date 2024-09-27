using CrowdControl.Common;
using ConnectorType = CrowdControl.Common.ConnectorType;

namespace CrowdControl.Games.Packs.TCGCardShopSimulator
{

    public class TCGCardShopSimulator : SimpleTCPPack
    {
        public override string Host => "127.0.0.1";

        public override ushort Port => 51337;

        public override ISimpleTCPPack.MessageFormat MessageFormat => ISimpleTCPPack.MessageFormat.CrowdControlLegacy;

        public TCGCardShopSimulator(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler) { }

        public override Game Game { get; } = new("TCG Card Shop Simulator", "TCGCardShopSimulator", "PC", ConnectorType.SimpleTCPServerConnector);

        public override EffectList Effects => new List<Effect>
        {
                new Effect("Toggle Lights", "lights") { Description = "Toggle the Shop Lights", Category = "Misc"},
                new Effect("Spawn Customer", "spawn") { Description = "Spawns a New Customer", Category = "Spawn"},
                new Effect("Give $100", "give_100") { Description = "Give the Player $100", Category = "Money"},
                new Effect("Give $1000", "give_1000") { Description = "Give the Player $1000", Category = "Money"},
                new Effect("Give $10000", "give_10000") { Description = "Give The Player $10000", Category = "Money"},
                new Effect("Take $100", "take_100") { Description = "Take away $100 from the Player", Category = "Money"},
                new Effect("Take $1000", "take_1000") { Description = "Take Away $1000 from the Player", Category = "Money"},
                new Effect("Take $10000", "take_10000") { Description = "Take Away $10000 from the Player", Category = "Money"},
                new Effect("Give 32 Common Packs", "give_common_pack_(32)") { Description = "Send the player a Common Pack (32)", Category = "Items"},
                new Effect("Give 64 Common Packs", "give_common_pack_(64)") { Description = "Send the player a Common Pack (64)", Category = "Items"},
                new Effect("Give Playmat (Drilceros)", "give_playmat_(drilceros)") { Description = "Send the player a Drilceros Playmat", Category = "Items"},
                new Effect("Give Common Box (4)", "give_common_box_(4)") { Description = "Send the player a Common Box (4)", Category = "Items"},
                new Effect("Give Common Box (8)", "give_common_box_(8)") { Description = "Send the player a Common Box (8)", Category = "Items"},
                new Effect("Give Rare Pack (32)", "give_rare_pack_(32)") { Description = "Send the player a Rare Pack (32)", Category = "Items"},
                new Effect("Give Rare Pack (64)", "give_rare_pack_(64)") { Description = "Send the player a Rare Pack (64)", Category = "Items"},
                new Effect("Give Rare Box (4)", "give_rare_box_(4)") { Description = "Send the player a Rare Box (4)", Category = "Items"},
                new Effect("Give Rare Box (8)", "give_rare_box_(8)") { Description = "Send the player a Rare Box (8)", Category = "Items"},
                new Effect("Give Epic Pack (32)", "give_epic_pack_(32)") { Description = "Send the player an Epic Pack (32)", Category = "Items"},
                new Effect("Give Epic Pack (64)", "give_epic_pack_(64)") { Description = "Send the player an Epic Pack (64)", Category = "Items"},
                new Effect("Give Epic Box (4)", "give_epic_box_(4)") { Description = "Send the player an Epic Box (4)", Category = "Items"},
                new Effect("Give Epic Box (8)", "give_epic_box_(8)") { Description = "Send the player an Epic Box (8)", Category = "Items"},
                new Effect("Give Legend Pack (32)", "give_legend_pack_(32)") { Description = "Send the player a Legend Pack (32)", Category = "Items"},
                new Effect("Give Legend Pack (64)", "give_legend_pack_(64)") { Description = "Send the player a Legend Pack (64)", Category = "Items"},
                new Effect("Give Legend Box (4)", "give_legend_box_(4)") { Description = "Send the player a Legend Box (4)", Category = "Items"},
                new Effect("Give Legend Box (8)", "give_legend_box_(8)") { Description = "Send the player a Legend Box (8)", Category = "Items"},
                new Effect("Give Playmat (Lunight)", "give_playmat_(lunight)") { Description = "Send the player a Lunight Playmat", Category = "Items"},
        };
    }
}
