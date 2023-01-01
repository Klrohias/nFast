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
        public bool Autoplay = false;

        private UnorderedList<PhiNote> _judgeNotes;

        private readonly UnorderedList<TouchDetail> _touches = new UnorderedList<TouchDetail>();
        private readonly UnorderedList<Tuple<PhiNote, PhiGamePlayer.JudgeResult>> _judgingHoldNotes = new();
        private int _touchCount = 0; // update each frame
        private float _currentTime = 0f;
        private const float NOTE_WIDTH = 2.5f * 0.88f;

        // event system active
        private Resolution _currentResolution;
        private float _eventSystemInactiveTime = float.PositiveInfinity;
        private const float EVENT_SYSTEM_ACTIVE_LAST = 3000f;
        public GameObject EventSystemObject;

        private class TouchDetail
        {
            public Touch RawTouch;
            public float[] LandDistances = null;
        }
        private void Start()
        {
            _judgeNotes = Player.JudgeNotes;
            _currentResolution = Screen.currentResolution;
        }
        private void Update()
        {
            if (!Player.GameRunning) return;

            _currentTime = Player.Timer.Time;
            TryDeactivateEventSystem();

            // update touch detail
            _touchCount = Input.touchCount;

            UpdateTouchDetails();

            ProcessJudgeNotes();

            UpdateHoldNotes();
        }

        private void UpdateHoldNotes()
        {
            for (var i = 0; i < _judgingHoldNotes.Length; i++)
            {
                var (note, judgeResult) = _judgingHoldNotes[i];
                if (note.EndBeats <= Player.CurrentBeats)
                {
                    PutJudgeResult(note, judgeResult);
                    _judgingHoldNotes.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!UpdateHoldNote(note))
                {
                    PutJudgeResult(note, PhiGamePlayer.JudgeResult.Miss);
                    _judgingHoldNotes.RemoveAt(i);
                    i--;
                }
            }
        }

        private bool UpdateHoldNote(PhiNote note)
        {
            var unitId = note.UnitId;
            for (var j = 0; j < _touchCount; j++)
            {
                var touch = _touches[j];

                if (MathF.Abs(ScreenAdapter.ToGameXPos(note.XPosition) -
                              touch.LandDistances[unitId]) > NOTE_WIDTH / 1.75f) continue;

                return true;
            }

            return false;
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

        private void TryDeactivateEventSystem()
        {
            if (_currentTime < _eventSystemInactiveTime) return;
            _eventSystemInactiveTime = float.PositiveInfinity;
            EventSystemObject.SetActive(false);
        }

        private void TryActivateEventSystem(Touch rawTouch)
        {
            if (!float.IsPositiveInfinity(_eventSystemInactiveTime)) return;
            var rawTouchPosition = rawTouch.position;
            if (rawTouchPosition.x >= _currentResolution.width * 0.25
                && rawTouchPosition.y <= _currentResolution.height * 0.75) return;
            
            _eventSystemInactiveTime = _currentTime + EVENT_SYSTEM_ACTIVE_LAST;
            EventSystemObject.SetActive(true);
        }

        private void UpdateTouchDetails()
        {
            for (int i = 0; i < _touchCount; i++)
            {
                if (_touches.Length <= i)
                {
                    _touches.Add(new TouchDetail
                    {
                        LandDistances = new float[Player.Units.Count]
                    });
                }

                var item = _touches[i];
                item.RawTouch = Input.GetTouch(i);

                TryActivateEventSystem(item.RawTouch);

                UpdateLandDistances(i);
            }
        }
        private void UpdateLandDistances(int touchIndex)
        {
            var lines = Player.Units;
            var touchDetail = _touches[touchIndex];
            var worldPos = Camera.main.ScreenToWorldPoint(touchDetail.RawTouch.position);

            for (var index = 0; index < lines.Count; index++)
            {
                var chartLine = lines[index];
                var linePos = Player.UnitObjects[(int)chartLine.UnitId].transform.position;
                var landPos = Vector2.Distance(GetLandPos(linePos, chartLine.Rotation, worldPos), linePos);
                touchDetail.LandDistances[index] = landPos;
            }
        }

        private void PutJudgeResult(PhiNote note, PhiGamePlayer.JudgeResult result)
        {
            _judgeNotes.Remove(note);
            // Debug.Log($"note judge {result}");
        }

        private void ProcessDragNote(PhiNote note)
        {
            if (note.JudgeTime > _currentTime) return;
            var unitId = note.UnitId;
            for (var i = 0; i < _touchCount; i++)
            {
                var touch = _touches[i];
                if (MathF.Abs(ScreenAdapter.ToGameXPos(note.XPosition) -
                                touch.LandDistances[unitId]) > NOTE_WIDTH / 1.75f) continue;

                PutJudgeResult(note, PhiGamePlayer.JudgeResult.Perfect);
                break;
            }
        }

        private void ProcessTapNote(PhiNote note)
        {
            var unitId = note.UnitId;
            for (var i = 0; i < _touchCount; i++)
            {
                var touch = _touches[i];
                if (touch.RawTouch.phase != TouchPhase.Began) continue;

                if (MathF.Abs(ScreenAdapter.ToGameXPos(note.XPosition) -
                              touch.LandDistances[unitId]) > NOTE_WIDTH / 1.75f) continue;
                
                var range = MathF.Abs(_currentTime - note.JudgeTime);
                if (range < PerfectJudgeRange) PutJudgeResult(note, PhiGamePlayer.JudgeResult.Perfect);
                else if (range < GoodJudgeRange) PutJudgeResult(note, PhiGamePlayer.JudgeResult.Good);
                else if (range < BadJudgeRange) PutJudgeResult(note, PhiGamePlayer.JudgeResult.Bad);
                break;
            }
        }

        private void ProcessFlickNote(PhiNote note)
        {
            var unitId = note.UnitId;
            for (var j = 0; j < _touchCount; j++)
            {
                var touch = _touches[j];
                if (touch.RawTouch.phase != TouchPhase.Moved) continue;

                if (MathF.Abs(ScreenAdapter.ToGameXPos(note.XPosition) -
                              touch.LandDistances[unitId]) > NOTE_WIDTH / 1.75f) continue;

                PutJudgeResult(note, PhiGamePlayer.JudgeResult.Perfect);
                break;
            }
        }

        private void ProcessHoldNote(PhiNote note)
        {
            var unitId = note.UnitId;
            for (var j = 0; j < _touchCount; j++)
            {
                var touch = _touches[j];

                if (MathF.Abs(ScreenAdapter.ToGameXPos(note.XPosition) -
                              touch.LandDistances[unitId]) > NOTE_WIDTH / 1.75f) continue;

                var range = MathF.Abs(_currentTime - note.JudgeTime);
                var judgeResult = PhiGamePlayer.JudgeResult.Miss;
                if (range < PerfectJudgeRange) judgeResult = PhiGamePlayer.JudgeResult.Perfect;
                else if (range < GoodJudgeRange) judgeResult = PhiGamePlayer.JudgeResult.Good;

                if (judgeResult == PhiGamePlayer.JudgeResult.Miss) break;

                _judgingHoldNotes.Add(new Tuple<PhiNote, PhiGamePlayer.JudgeResult>(note, PhiGamePlayer.JudgeResult.Miss));
                _judgeNotes.Remove(note);
                break;
            }
        }

        private void ProcessJudgeNote(PhiNote note)
        {
            if (Autoplay && note.JudgeTime <= _currentTime)
            {
                PutJudgeResult(note, PhiGamePlayer.JudgeResult.Perfect);
                return;
            }

            switch (note.Type)
            {
                case NoteType.Tap:
                    ProcessTapNote(note);
                    break;
                case NoteType.Flick:
                    ProcessFlickNote(note);
                    break;
                case NoteType.Hold:
                    ProcessHoldNote(note);
                    break;
                case NoteType.Drag:
                    ProcessDragNote(note);
                    break;
            }
        }

        private void ProcessJudgeNotes()
        {
            lock (_judgeNotes)
            {
                for (var i = 0; i < _judgeNotes.Length; i++)
                {
                    var item = _judgeNotes[i];

                    if (item.JudgeTime - _currentTime > BadJudgeRange) continue;
                    if (_currentTime - item.JudgeTime > BadJudgeRange)
                    {
                        PutJudgeResult(item, PhiGamePlayer.JudgeResult.Miss);
                        continue;
                    }

                    ProcessJudgeNote(item);
                }
            }
        }
    }
}