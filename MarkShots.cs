using UnityEngine;
using System.Collections.Generic;
using CompanionServer.Handlers;
using System.Linq;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("MarkShots", "bmgjet", "1.0.0")]
    [Description("Shows on map where there has been gun fire.")]

    public class MarkShots : RustPlugin
    {
        //Settings Start
        Color markercolor = Color.white;
        float markeralpha = 0.3f;
        float markerradius = 1f;
        int maxmarkersperplayer = 5;
        int distancebetweenmarkers = 100;
        int markerdecaytime = 60;
        int markerrefreshtime = 2;
        //Settings End

        const string PermMap = "MarkShots.enabled";
        private Coroutine _routine;
        public List<MapMarkerGenericRadius> markers = new List<MapMarkerGenericRadius>();
        public Dictionary<Vector3,ulong> shots = new Dictionary<Vector3, ulong>();

        private void Init()
        {
            permission.RegisterPermission(PermMap, this);
        }

        void OnServerInitialized()
        {
            if (BasePlayer.activePlayerList.Count != 0 && _routine == null)
            {
                RunMap();
            }
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            addshot(player);
        }

        private void OnRocketLaunched(BasePlayer player)
        {
            addshot(player);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (_routine == null)
            {
                RunMap();
            }
        }

        void Unload()
        {
            MarkerDisplayingDelete(null, null, null);
            if (_routine != null)
            {
                ServerMgr.Instance.StopCoroutine(_routine);
            }
        }

        IEnumerator MapRoutine()
        {
            do
            {
                DrawMap();
                yield return CoroutineEx.waitForSeconds(markerrefreshtime);
            } while (true);
        }

        void MarkerDisplayingDelete(BasePlayer player, string command, string[] args)
        {
            foreach (var m in markers)
            {
                if (m != null)
                {
                    m.Kill();
                    m.SendUpdate();
                }
            }
            markers.Clear();
        }

        object CanNetworkTo(MapMarkerGenericRadius marker, BasePlayer player)
        {
            if (!markers.Contains(marker) || (player.IPlayer.HasPermission(PermMap) && markers.Contains(marker)))
            {
                return null;
            }
            return false;
        }

        private void addshot(BasePlayer player)
        {
            Vector3 pos = player.transform.position;
            if (!shots.ContainsKey(pos))
            {
            int spammed = 0;
            foreach (KeyValuePair<Vector3, ulong> spamcheck in shots)
            {
                if (spamcheck.Value == player.userID)
                    spammed++;

                if(Vector3.Distance(pos,spamcheck.Key) < distancebetweenmarkers || spammed > maxmarkersperplayer)
                {
                    return;
                }
            }
                shots.Add(pos, player.userID);
                timer.Once(markerdecaytime, () =>
                {
                    shots.Remove(pos);
                });
            }
        }

        void RunMap()
        {
            _routine = ServerMgr.Instance.StartCoroutine(MapRoutine());
            Puts("ShotsMap Thread Started");
        }

        void DrawMap()
        {
            MarkerDisplayingDelete(null, null, null);
            foreach (KeyValuePair<Vector3, ulong> gs in shots.ToList())
            {
                MapMarkerGenericRadius MapMarkerCustom;
                MapMarkerCustom = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", gs.Key) as MapMarkerGenericRadius;
                MapMarkerCustom.alpha = markeralpha;
                MapMarkerCustom.color1 = markercolor;
                MapMarkerCustom.color2 = markercolor;
                MapMarkerCustom.radius = markerradius;
                markers.Add(MapMarkerCustom);
            }
            foreach (var m in markers)
            {
                    m.Spawn();
                    MapMarker.serverMapMarkers.Remove(m);
                    m.SendUpdate();
            }
        }
    }
}