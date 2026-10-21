using GorillaTagScripts;
using Photon.Pun;
using ShibaGTGenesisReborn.Libs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        private static float blockTime;

        private static BuilderTable GetBlocksTable() =>
            BuilderTable.TryGetBuilderTableForZone(GTZone.monkeBlocks, out BuilderTable table) && table.GetTableState() == BuilderTable.TableState.Ready ? table : null;

        private static IEnumerable<BuilderPiece> FreeBlocks(BuilderTable table) =>
            table.pieces.Where(piece => piece != null && !piece.IsPrivatePlot() &&
                (piece.state == BuilderPiece.State.AttachedAndPlaced || piece.state == BuilderPiece.State.Dropped || piece.state == BuilderPiece.State.AttachedToDropped));

        private static bool BlockTick(out BuilderTable table)
        {
            table = Time.time > blockTime ? GetBlocksTable() : null;
            if (table == null) return false;
            blockTime = Time.time + 0.1f;
            return true;
        }

        private static void DropBlock(BuilderTable table, BuilderPiece piece, Vector3 position, Vector3 velocity) =>
            table.builderNetworking.photonView.RPC("PieceDroppedRPC", RpcTarget.All, table.builderNetworking.CreateLocalCommandId(), piece.pieceId,
                position, Random.rotation, velocity, Random.insideUnitSphere * 5f, PhotonNetwork.LocalPlayer);

        public static void BlockGun()
        {
            GunLib.StartGun(() =>
            {
                if (!BlockTick(out BuilderTable table)) return;
                BuilderPiece piece = FreeBlocks(table).OrderBy(_ => Random.value).FirstOrDefault();
                if (piece == null) return;

                Vector3 hand = GorillaTagger.Instance.rightHandTransform.position;
                DropBlock(table, piece, hand, (GunLib.spherepointer.transform.position - hand).normalized * BuilderTable.MAX_DROP_VELOCITY);
            }, false);
        }

        public static void BlockRain()
        {
            if (!BlockTick(out BuilderTable table)) return;
            BuilderPiece piece = FreeBlocks(table).OrderBy(_ => Random.value).FirstOrDefault();
            if (piece == null) return;

            Vector2 offset = Random.insideUnitCircle * 8f;
            DropBlock(table, piece, GorillaTagger.Instance.bodyCollider.transform.position + new Vector3(offset.x, 10f, offset.y), Vector3.down * 5f);
        }

        public static void BringAllBlocks()
        {
            if (!BlockTick(out BuilderTable table)) return;
            Vector3 center = GorillaTagger.Instance.bodyCollider.transform.position;
            foreach (BuilderPiece piece in FreeBlocks(table).Where(piece => Vector3.Distance(piece.transform.position, center) > 3f).Take(3).ToArray())
                DropBlock(table, piece, center + Random.insideUnitSphere * 1.5f + Vector3.up, Vector3.zero);
        }

        public static void DeleteBlockGun()
        {
            GunLib.StartGun(() =>
            {
                if (!BlockTick(out BuilderTable table)) return;
                Vector3 point = GunLib.spherepointer.transform.position;
                BuilderPiece piece = FreeBlocks(table).OrderBy(p => Vector3.Distance(p.transform.position, point)).FirstOrDefault();
                if (piece != null && Vector3.Distance(piece.transform.position, point) < 1f)
                    table.RequestRecyclePiece(piece, true, -1);
            }, false);
        }

        public static void DeleteAllBlocks()
        {
            if (!BlockTick(out BuilderTable table)) return;
            foreach (BuilderPiece piece in FreeBlocks(table).Take(3).ToArray())
                table.RequestRecyclePiece(piece, true, -1);
        }
    }
}
