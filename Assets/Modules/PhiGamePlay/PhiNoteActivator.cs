using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteActivator : MonoBehaviour
    {
        public PhiGamePlayer Player;
        public ScreenAdapter ScreenAdapter;
        private Queue<PhiNote> _newNotes;
        private List<IPhiUnitWrapper> _unitWrappers;
        private readonly UnorderedList<IPhiNoteWrapper> _noteWrappers = new();
        private void Start()
        {
            _newNotes = Player.NewNotes;
            _unitWrappers = Player.UnitWrappers;
        }
        private void Update()
        {
            if (!Player.GameRunning) return;

            lock (_newNotes)
            {
                ActivateDisplayNote();
            }

            DeactivateNotes();
        }

        private void DeactivateNotes()
        {
            var currentBeats = Player.CurrentBeats;
            for (int i = 0; i < _noteWrappers.Length; i++)
            {
                var item = _noteWrappers[i].Note;
                if (item.EndBeats > currentBeats) continue;

                if (item.Type == NoteType.Hold) Player.OnNoteFinalize((PhiHoldNoteWrapper) _noteWrappers[i]);
                else Player.OnNoteFinalize((PhiNoteWrapper) _noteWrappers[i]);

                _noteWrappers.RemoveAt(i);
                i--;
            }
        }

        private void ActivateDisplayNote()
        {
            while (_newNotes.TryDequeue(out var note))
            {

                var noteObj = note.NoteGameObject;
                if (noteObj != null) continue;

                noteObj = note.NoteGameObject =
                    note.Type == NoteType.Hold
                        ? Player.HoldNotePool.RequestObject()
                        : Player.NotePool.RequestObject();

                var lineWrapper = _unitWrappers[(int) note.UnitId];

                var typedWrapper = (PhiLineWrapper) lineWrapper;
                noteObj.transform.parent = note.ReverseDirection
                    ? typedWrapper.DownNoteViewport
                    : typedWrapper.UpNoteViewport;

                var localPos = noteObj.transform.localPosition;
                var noteYOffset = ScreenAdapter.ToGameYPos(note.YPosition);
                localPos.y = note.NoteHeight + noteYOffset;
                localPos.x = ScreenAdapter.ToGameXPos(note.XPosition);
                noteObj.transform.localPosition = localPos;

                var noteWrapper = noteObj.GetComponent<IPhiNoteWrapper>();
                noteWrapper.Note = note;
                _noteWrappers.Add(noteWrapper);
                noteWrapper.NoteStart();
            }
        }
    }
}