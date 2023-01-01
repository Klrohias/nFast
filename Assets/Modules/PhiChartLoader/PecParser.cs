using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Klrohias.NFast.PhiChartLoader
{
    public class PecParser
    {
        private StreamReader _reader;
        private readonly PhiChart _resultChart = new();
        private float _offset = 0f;
        private readonly List<BpmEvent> _bpmEvents = new();
        private readonly List<PhiNote> _notes = new();
        private readonly List<PhiUnit> _units = new();
        private readonly List<UnitEvent> _unitEvents = new();
        private readonly List<Vector2> _lastMovePositions = new();
        private readonly List<float> _lastRotation = new();
        private readonly List<float> _lastAlpha = new();
        
        private const float RES_WIDTH = 1024f;
        private const float RES_NOTE_SPACE_WIDTH = 1024f;
        private const float RES_HEIGHT = 700f;

        private const float TARGET_WIDTH = 675f;
        private const float TARGET_HEIGHT = 450f;
        public PhiChart Result => _resultChart;
        public PecParser(Stream inStream)
        {
            _reader = new StreamReader(inStream);
        }

        private float ScaleXPos(float x) => (x - RES_WIDTH) / RES_WIDTH * TARGET_WIDTH;
        private float ScaleYPos(float x) => (x - RES_HEIGHT) / RES_HEIGHT * TARGET_HEIGHT;
        private float ScaleNoteXPos(float x) => x / RES_NOTE_SPACE_WIDTH * TARGET_WIDTH;

        private void ParseBpmEvent(string instruction)
        {
            var segments = instruction.Split(' ');
            if (segments.Length != 3) 
                throw new InvalidDataException($"BpmEvent with {segments.Length} parameters");

            // segments[0] = bp
            // segments[1] = beats
            // segments[2] = bpm

            _bpmEvents.Add(BpmEvent.Create(float.Parse(segments[1])
                , float.Parse(segments[2])));
        }

        private void ParseNote(string instruction)
        {
            var lines = instruction.Split('\n');
            var result = new PhiNote();

            // parse note type
            // lines[0][1] = note type(number string)

            var typeNumber = lines[0][1] - 48;
            result.Type = typeNumber switch
            {
                1 => NoteType.Tap,
                2 => NoteType.Hold,
                3 => NoteType.Flick,
                4 => NoteType.Drag,
                _ => throw new Exception($"Unknown note type {typeNumber}")
            };

            var segments = lines[0][3..].Split(' ');

            // segments[0] = line index (unit id)
            // segments[1] = begin beats
            // if it is a hold note, segments[2] should be end beats
            // segments[2] = x position
            // segments[3] = is fake note

            var segmentIndex = 0;

            result.UnitId = uint.Parse(segments[segmentIndex++]);
            result.BeginBeats = float.Parse(segments[segmentIndex++]);
            result.EndBeats = result.Type == NoteType.Hold ? 
                float.Parse(segments[segmentIndex++]) : result.BeginBeats;
            result.XPosition = ScaleNoteXPos(float.Parse(segments[segmentIndex++]));
            result.ReverseDirection = segments[segmentIndex++] == "0";
            result.IsFakeNote = segments[segmentIndex] == "1";

            // speed and size is not supported now

            _notes.Add(result);
        }

        private void ParseCvEvent(uint unitId, string[] segments)
        {
            // speed event
            var beats = float.Parse(segments[2]);
            var speed = float.Parse(segments[3]);
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.Speed,
                BeginBeats = beats,
                EndBeats = beats,
                BeginValue = speed,
                EndValue = speed,
                EasingFunc = EasingFunction.Linear,
                UnitId = unitId,
                EasingFuncRange = (0, 1)
            });
        }
        private void ParseCmEvent(uint unitId, string[] segments)
        {
            // move (linear interpolation)
            var beginBeats = float.Parse(segments[2]);
            var endBeats = float.Parse(segments[3]);
            var xPos = float.Parse(segments[4]);
            var yPos = float.Parse(segments[5]);
            var easingType = int.Parse(segments[6]);

            var lastMovePos = _lastMovePositions[(int) unitId];
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.MoveX,
                BeginBeats = beginBeats,
                EndBeats = endBeats,
                BeginValue = ScaleXPos(lastMovePos.x),
                EndValue = ScaleXPos(xPos),
                EasingFuncRange = (0,1),
                UnitId = unitId,
                EasingFunc = PezLineEvent.ToNFastEasingFunction(easingType),
            });
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.MoveY,
                BeginBeats = beginBeats,
                EndBeats = endBeats,
                BeginValue = ScaleYPos(lastMovePos.y),
                EndValue = ScaleYPos(yPos),
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = PezLineEvent.ToNFastEasingFunction(easingType),
            });
            lastMovePos.x = xPos;
            lastMovePos.y = yPos;
            _lastMovePositions[(int) unitId] = lastMovePos;
        }
        private void ParseCpEvent(uint unitId, string[] segments)
        {
            // move
            var beginBeats = float.Parse(segments[2]);
            var xPos = float.Parse(segments[3]);
            var yPos = float.Parse(segments[4]);

            var lastMovePos = _lastMovePositions[(int)unitId];
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.MoveX,
                BeginBeats = beginBeats,
                EndBeats = beginBeats,
                BeginValue = ScaleXPos(lastMovePos.x),
                EndValue = ScaleXPos(xPos),
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = EasingFunction.Linear
            });
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.MoveY,
                BeginBeats = beginBeats,
                EndBeats = beginBeats,
                BeginValue = ScaleYPos(lastMovePos.y),
                EndValue = ScaleYPos(yPos),
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = EasingFunction.Linear
            });
            lastMovePos.x = xPos;
            lastMovePos.y = yPos;
            _lastMovePositions[(int)unitId] = lastMovePos;
        }
        private void ParseCdEvent(uint unitId, string[] segments)
        {
            // rotation
            var beginBeats = float.Parse(segments[2]);
            var rotation = float.Parse(segments[3]);
            
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.Rotate,
                BeginBeats = beginBeats,
                EndBeats = beginBeats,
                BeginValue = rotation,
                EndValue = rotation,
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = EasingFunction.Linear
            });

            _lastRotation[(int) unitId] = rotation;
        }
        private void ParseCrEvent(uint unitId, string[] segments)
        {
            // rotation
            var beginBeats = float.Parse(segments[2]);
            var endBeats = float.Parse(segments[3]);
            var rotation = float.Parse(segments[4]);
            var easingType = int.Parse(segments[5]);
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.Rotate,
                BeginBeats = beginBeats,
                EndBeats = endBeats,
                BeginValue = _lastRotation[(int)unitId],
                EndValue = rotation,
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = PezLineEvent.ToNFastEasingFunction(easingType),
            });

            _lastRotation[(int)unitId] = rotation;
        }

        private void ParseCaEvent(uint unitId, string[] segments)
        {
            // rotation
            var beginBeats = float.Parse(segments[2]);
            var alpha = float.Parse(segments[3]);

            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.Alpha,
                BeginBeats = beginBeats,
                EndBeats = beginBeats,
                BeginValue = alpha,
                EndValue = alpha,
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = EasingFunction.Linear
            });

            _lastAlpha[(int)unitId] = alpha;
        }
        private void ParseCfEvent(uint unitId, string[] segments)
        {
            // rotation
            var beginBeats = float.Parse(segments[2]);
            var endBeats = float.Parse(segments[3]);
            var alpha = float.Parse(segments[4]);
            _unitEvents.Add(new UnitEvent
            {
                Type = UnitEventType.Alpha,
                BeginBeats = beginBeats,
                EndBeats = endBeats,
                BeginValue = _lastAlpha[(int)unitId],
                EndValue = alpha,
                EasingFuncRange = (0, 1),
                UnitId = unitId,
                EasingFunc = EasingFunction.Linear
            });

            _lastAlpha[(int)unitId] = alpha;
        }
        private void ResizeLine(uint lineId)
        {
            while (_units.Count - 1 < lineId)
            {
                _units.Add(new PhiUnit
                {
                    UnitId = (uint)_units.Count,
                    ParentUnitId = -1,
                    Type = PhiUnitType.Line
                });
            }

            while (_lastMovePositions.Count - 1 < lineId)
            {
                _lastMovePositions.Add(Vector2.zero);
            }

            while (_lastRotation.Count - 1 < lineId)
            {
                _lastRotation.Add(0f);
            }

            while (_lastAlpha.Count - 1 < lineId)
            {
                _lastAlpha.Add(0f);
            }
        }

        private void ParseLine(string instruction)
        {
            var type = instruction[1];
            var segments = instruction.Split(' ');
            var unitId = uint.Parse(segments[1]);

            ResizeLine(unitId);

            switch (type)
            {
                case 'v':
                {
                    ParseCvEvent(unitId, segments);
                    break;
                }
                case 'm':
                {
                    ParseCmEvent(unitId, segments);
                    break;
                }
                case 'p':
                {
                    ParseCpEvent(unitId, segments);
                    break;
                }
                case 'd':
                {
                    ParseCdEvent(unitId, segments);
                    break;
                }
                case 'r':
                {
                    ParseCrEvent(unitId, segments);
                    break;
                }
                case 'a':
                {
                    ParseCaEvent(unitId, segments);
                    break;
                }
                case 'f':
                {
                    ParseCfEvent(unitId, segments);
                    break;
                }
            }
        }

        private void ParseInstruction(string instruction)
        {
            switch (instruction[0])
            {
                case 'b':
                {
                    ParseBpmEvent(instruction);
                    break;
                }
                case 'n':
                {
                    ParseNote(instruction);
                    break;
                }
                case 'c':
                {
                    ParseLine(instruction);
                    break;
                }
            }
        }

        public async Task ParsePhiChart()
        {
            _offset = float.Parse(await _reader.ReadLineAsync());
            
            while (!_reader.EndOfStream)
            {
                var line = await _reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith('n'))
                    line += $"\n{await _reader.ReadLineAsync()}" +
                            $"\n{await _reader.ReadLineAsync()}";

                ParseInstruction(line);
            }

            _resultChart.BpmEvents = _bpmEvents.ToArray();
            _resultChart.Notes = _notes.ToArray();
            _resultChart.Units = _units.ToArray();
            _resultChart.UnitEvents = _unitEvents.ToArray();
        }
    }
}