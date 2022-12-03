using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Klrohias.NFast.ChartLoader;
using Klrohias.NFast.ChartLoader.Pez;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.Utilities;
using UnityEngine;

public class GamePlayer : MonoBehaviour
{
    async void Start()
    {
        // load chart file
        var loadInstruction = NavigationService.Get().ExtraData as string;
        if (loadInstruction == null) throw new InvalidOperationException("failed to load: unknown");
        await LoadChart(loadInstruction);
        GameBegin();
    }

    async Task LoadChart(string filePath)
    {
        // TODO: support pez only now
        PezRoot pezChart = null;
        Chart chart = null;
        // 高算力操作移到另一个线程操作
        await Async.RunOnThread(() =>
        {
            pezChart = PezLoader.LoadPezChart(filePath);
            chart = pezChart.ToChart();
        });
        // TODO: 后面要加载音乐等资源的话，将ToChart啥的一起分开来，3线程同时工作
    }

    void GameBegin()
    {

    }

    void Update()
    {
        
    }
}
