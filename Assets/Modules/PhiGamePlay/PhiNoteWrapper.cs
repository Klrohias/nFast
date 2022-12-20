using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader.NFast;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteWrapper : MonoBehaviour
    {
        private bool isRunning = false;
        public PhiGamePlayer Player;
        private ChartNote note;
        private Quaternion ZeroRotation = Quaternion.Euler(0, 0, 0);
        public void NoteStart(ChartNote note)
        {
            isRunning = true;
            this.note = note;
            transform.rotation = ZeroRotation;
        }

        void Update()
        {
            if (!isRunning) return;
            if (Player.CurrentBeats >= note.EndTime.Beats)
            {
                isRunning = false;
                Player.OnNoteFinalize(this);
                return;
            }

            var localPos = transform.localPosition;
            localPos.y = (note.StartTime.Beats - Player.CurrentBeats) * 6f;
            transform.localPosition = localPos;
        }
    }
}