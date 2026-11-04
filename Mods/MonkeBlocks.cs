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
        private static BuilderTable GetBlocksTable() =>
            BuilderTable.TryGetBuilderTableForZone(GTZone.monkeBlocks, out BuilderTable table) && table.GetTableState() == BuilderTable.TableState.Ready ? table : null;

        private static IEnumerable<BuilderPiece> FreeBlocks(BuilderTable table) =>
            table.pieces.Where(piece => piece != null && !piece.IsPrivatePlot() &&
                (piece.state == BuilderPiece.State.AttachedAndPlaced || piece.state == BuilderPiece.State.Dropped || piece.state == BuilderPiece.State.AttachedToDropped));

        private static float blockTime;

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

        public static void BlockTornado()
        {
            if (!BlockTick(out BuilderTable table)) return;
            Vector3 center = GorillaTagger.Instance.bodyCollider.transform.position;
            foreach (BuilderPiece piece in FreeBlocks(table).OrderBy(_ => Random.value).Take(3).ToArray())
            {
                float angle = Random.value * Mathf.PI * 2f, height = Random.Range(0f, 6f), radius = 1f + height * 0.5f;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                DropBlock(table, piece, center + outward * radius + Vector3.up * height, Vector3.Cross(Vector3.up, outward) * 8f + Vector3.up * 4f);
            }
        }

        private static int orbitIndex;

        public static void BlockOrbit()
        {
            if (!BlockTick(out BuilderTable table)) return;
            Vector3 center = GorillaTagger.Instance.bodyCollider.transform.position;
            BuilderPiece[] pieces = FreeBlocks(table).OrderBy(piece => Vector3.Distance(piece.transform.position, center)).Take(12).ToArray();
            for (int k = 0; k < 3 && pieces.Length > 0; k++)
            {
                int i = orbitIndex++ % pieces.Length;
                float angle = Time.time * 2f + i * Mathf.PI * 2f / pieces.Length;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                DropBlock(table, pieces[i], center + outward * 1.5f + Vector3.up * 0.5f, Vector3.Cross(Vector3.up, outward) * 3f + Vector3.up * 2f);
            }
        }

        public static void BlockFountain()
        {
            if (!BlockTick(out BuilderTable table)) return;
            Vector3 hand = GorillaTagger.Instance.rightHandTransform.position;
            foreach (BuilderPiece piece in FreeBlocks(table).OrderBy(_ => Random.value).Take(2).ToArray())
                DropBlock(table, piece, hand + Vector3.up * 0.3f, (Vector3.up * 3f + Random.insideUnitSphere).normalized * BuilderTable.MAX_DROP_VELOCITY);
        }

        public static void BlockExplosionGun()
        {
            GunLib.StartGun(() =>
            {
                if (!BlockTick(out BuilderTable table)) return;
                Vector3 point = GunLib.spherepointer.transform.position;
                foreach (BuilderPiece piece in FreeBlocks(table).OrderBy(_ => Random.value).Take(3).ToArray())
                {
                    Vector3 direction = (Random.onUnitSphere + Vector3.up).normalized;
                    DropBlock(table, piece, point + direction * 0.3f, direction * BuilderTable.MAX_DROP_VELOCITY);
                }
            }, false);
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
