using System.Collections;
using System.Collections.Generic;
using Codice.CM.Common;
using Klrohias.NFast.PhiChartLoader.NFast;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteWrapper : MonoBehaviour
    {
        private bool isRunning = false;
        public PhiGamePlayer Player;
        private ChartNote note;
        private static Quaternion ZeroRotation = Quaternion.Euler(0, 0, 0);
        private PhiLineWrapper line;

        public void NoteStart(ChartNote note, PhiLineWrapper line)
        {
            isRunning = true;
            this.note = note;
            this.line = line;
            transform.localRotation = ZeroRotation;
        }

        void Update()
        {
            if (!isRunning) return;
            if (Player.CurrentBeats >= note.EndTime.Beats)
            {
                isRunning = false;
                Player.OnNoteFinalize(this);
                Debug.Log("note finalize");
                return;
            }
            var localPos = transform.localPosition;
            localPos.y = note.YPosition - line.Line.YPosition;
            transform.localPosition = localPos;
        }
    }
}