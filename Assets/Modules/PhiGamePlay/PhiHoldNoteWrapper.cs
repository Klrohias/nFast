using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader.NFast;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiHoldNoteWrapper : MonoBehaviour, IPhiNoteWrapper
    {
        private bool _isRunning = false;
        public Transform TopHead;
        public Transform BottomHead;
        public Transform Body;
        private ChartNote _note;
        internal PhiGamePlayer Player;
        private ChartLine _line;
        private const float BODY_HEIGHT = 0.04f;
        private const float Y_SCALE = 2.5f;
        private void Update()
        {
            if (!_isRunning) return;
            if (Player.CurrentBeats >= _note.EndTime)
            {
                _isRunning = false;
                Player.OnNoteFinalize(this);
                return;
            }
            var localPos = transform.localPosition;
            var newPosY = _note.YPosition - _line.YPosition;
            localPos.y = newPosY;
            transform.localPosition = localPos;
        }

        public void NoteStart(ChartNote note)
        {
            _isRunning = true;
            this._note = note;
            this._line = Player.Lines[(int)note.LineId];
            transform.localRotation = PhiNoteWrapper.ZeroRotation;
            BottomHead.localPosition = Vector3.zero;
            Body.localScale = new Vector3(1f, note.Height / BODY_HEIGHT / Y_SCALE, 1f);
            TopHead.localPosition = Vector3.up * note.Height / Y_SCALE;
            Body.localPosition = Vector3.up * (note.Height / Y_SCALE / 2f);
        }
    }
}
