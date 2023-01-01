using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Klrohias.NFast.Native;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.PhiGamePlay;
using Klrohias.NFast.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class PlayUIController : MonoBehaviour
    {
        public Button BackButton;
        public PhiGamePlayer Player;

        private void Start()
        {
            BackButton.onClick.AddListener(() => NavigationService.Get().Back());

            StartGame();
        }

        private async void StartGame()
        {
            // load chart
            var stopwatch = Stopwatch.StartNew();
            var cachePath = OSService.Get().CachePath;
            var chartPath = NavigationService.Get().ExtraData as string;
            if (chartPath == null) throw new InvalidOperationException("Invalid chartPath");
            var chart = await ChartLoader.LoadChartAsync(chartPath, cachePath);
            $"parse chart {stopwatch.ElapsedMilliseconds}ms".Log();

            // run game
            await Player.LoadChart(chart);
            Player.RunGame();
        }
    }
}