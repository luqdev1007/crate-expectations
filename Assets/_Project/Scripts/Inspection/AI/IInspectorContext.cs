using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Stations;
using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    public interface IInspectorContext
    {
        InspectionDefinition Definition { get; }

        InspectorProfile Profile { get; }

        InspectorLinesDefinition Lines { get; }

        CargoPlacementZone Zone { get; }

        InspectorMotor Motor { get; }

        IInspectorVoice Voice { get; }

        ExamineFocusMarker Focus { get; }

        Transform Post { get; }

        Transform ExaminePoint { get; }

        IReadOnlyList<Transform> PatrolPoints { get; }

        CancellationToken StateToken { get; }
        bool AwaitsInspection { get; }

        CargoBox ApproachingCargo { get; }

        InspectionCase Case { get; }

        Verdict OpenCase(CargoBox cargo);

        void CloseCase();

        void GoTo(InspectorPhase phase);
    }
}
