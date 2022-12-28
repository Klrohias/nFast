using System;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiJudgeHandler : MonoBehaviour
    {
        public PhiGamePlayer Player;
        public ScreenAdapter ScreenAdapter;

        public float PerfectJudgeRange = 80f;
        public float GoodJudgeRange = 150f;
        public float BadJudgeRange = 350f;

        private UnorderedList<PhiNote> _judgeNotes;

        private readonly UnorderedList<TouchDetail> _touches = new UnorderedList<TouchDetail>();
        private int _touchCount = 0; // update each frame

        private const float NOTE_WIDTH = 2.5f * 0.88f;
        private class TouchDetail
        {
            public Touch RawTouch;
            public float[] LandDistances = null;
        }
        private void Start()
        {
            _judgeNotes = Player.JudgeNotes;
        }
        private void Update()
        {
            if (!Player.GameRunning) return;

            // update touch detail
            _touchCount = Input.touchCount;
            UpdateTouchDetails();
            ProcessJudgeNotes();
        }

        private static Vector2 GetLandPos(Vector2 lineOrigin, float rotation, Vector2 touchPos)
        {
            if (rotation % MathF.PI == 0f) return new Vector2(touchPos.x, lineOrigin.y);
            var k = MathF.Tan(rotation);
            var b = lineOrigin.y - k * lineOrigin.x;
            var k2 = -1 / k;
            var b2 = touchPos.y - k2 * touchPos.x;
            var x = (b2 - b) / (k - k2);
            var y = k * x + b;
            return new Vector2(x, y);
        }
        private void UpdateTouchDetails()
        {
            for (int i = 0; i < _touchCount; i++)
            {
                if (_touches.Length <= i)
                {
                    _touches.Add(new TouchDetail
                    {
                        LandDistances = new float[Player.Lines.Count]
                    });
                }

                var item = _touches[i];
                item.RawTouch = Input.GetTouch(i);
                UpdateLandDistances(i);
            }
        }
        private void UpdateLandDistances(int touchIndex)
        {
            var lines = Player.Lines;
            var touchDetail = _touches[touchIndex];
            var worldPos = Camera.main.ScreenToWorldPoint(touchDetail.RawTouch.position);

            for (var index = 0; index < lines.Count; index++)
            {
                var chartLine = lines[index];
                var linePos = Player.LineObjects[(int)chartLine.LineId].transform.position;
                var landPos = Vector2.Distance(GetLandPos(linePos, chartLine.Rotation, worldPos), linePos);
                touchDetail.LandDistances[index] = landPos;
            }
        }
        
        private void ProcessJudgeNotes()
        {
            var currentTime = Player.Timer.Time;
            for (int i = 0; i < Player.JudgeNotes.Length; i++)
            {
                var item = _judgeNotes[i];

                if (item.JudgeTime - currentTime > BadJudgeRange) continue;

                if (currentTime - item.JudgeTime > BadJudgeRange)
                {
                    // TODO: miss
                    _judgeNotes.RemoveAt(i);
                    i--;
                    continue;
                }

                var lineId = item.LineId;

                switch (item.Type)
                {
                    case NoteType.Tap:
                    {
                        break;
                    }
                    case NoteType.Flick:
                    {
                        break;
                    }
                    case NoteType.Hold:
                    {
                        break;
                    }
                    case NoteType.Drag:
                    {
                        if (item.JudgeTime > currentTime) continue;
                        for (int j = 0; j < _touchCount; j++)
                        {
                            if (MathF.Abs(ScreenAdapter.ToGameXPos(item.XPosition) -
                                          _touches[j].LandDistances[lineId]) < NOTE_WIDTH / 1.75f)
                            {
                                // TODO: perfect
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}