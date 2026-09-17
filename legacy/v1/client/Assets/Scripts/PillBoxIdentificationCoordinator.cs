using System;

namespace EZDose.CheckPillBox
{
    public enum PillBoxIdentificationSource
    {
        Barcode,
        Rfid
    }

    public sealed class PillBoxIdentificationResult
    {
        public PillBoxIdentificationSource Source { get; }
        public string RawIdentifier { get; }
        public string PatientId { get; }

        public PillBoxIdentificationResult(
            PillBoxIdentificationSource source,
            string rawIdentifier,
            string patientId)
        {
            Source = source;
            RawIdentifier = rawIdentifier ?? string.Empty;
            PatientId = patientId ?? string.Empty;
        }
    }

    /// <summary>
    /// Combines camera barcode and RFID identities into one scan session.
    /// Either source may verify the box; conflicting valid identities revoke trust.
    /// </summary>
    public sealed class PillBoxIdentificationCoordinator
    {
        private readonly Func<string, string> resolvePatientIdByRfid;
        private string expectedPatientId;
        private PillBoxIdentificationResult observedResult;

        public bool IsActive { get; private set; }
        public bool IsAutoMode { get; private set; }
        public bool HasSeenRfid { get; private set; }
        public bool IsRfidPresent { get; private set; }
        public string CurrentRfidUid { get; private set; }
        public PillBoxIdentificationResult VerifiedResult { get; private set; }

        public event Action<PillBoxIdentificationResult> Verified;
        public event Action<PillBoxIdentificationResult, string> Mismatch;
        public event Action<string> UnknownRfid;
        public event Action<string> RfidRemoved;
        public event Action<string, string> RfidChanged;
        public event Action<PillBoxIdentificationResult, PillBoxIdentificationResult> Conflict;

        public PillBoxIdentificationCoordinator(Func<string, string> resolvePatientIdByRfid)
        {
            this.resolvePatientIdByRfid = resolvePatientIdByRfid;
        }

        public void StartSession(string expectedPatientId, bool autoMode)
        {
            this.expectedPatientId = (expectedPatientId ?? string.Empty).Trim();
            IsAutoMode = autoMode;
            IsActive = true;
            HasSeenRfid = false;
            IsRfidPresent = false;
            CurrentRfidUid = null;
            VerifiedResult = null;
            observedResult = null;
        }

        public void StopSession()
        {
            IsActive = false;
            IsRfidPresent = false;
            CurrentRfidUid = null;
            VerifiedResult = null;
            observedResult = null;
        }

        public void HandleBarcode(string decoded)
        {
            if (!IsActive)
            {
                return;
            }

            string patientId = CheckPillBoxController.ParsePatientIdFromBarcode(decoded).Trim();
            HandleIdentity(new PillBoxIdentificationResult(
                PillBoxIdentificationSource.Barcode,
                decoded,
                patientId));
        }

        public void HandleRfidPlaced(string uid)
        {
            if (!IsActive || string.IsNullOrWhiteSpace(uid))
            {
                return;
            }

            uid = uid.Trim().ToUpperInvariant();
            HasSeenRfid = true;
            IsRfidPresent = true;
            CurrentRfidUid = uid;

            string patientId = resolvePatientIdByRfid?.Invoke(uid);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                UnknownRfid?.Invoke(uid);
                return;
            }

            HandleIdentity(new PillBoxIdentificationResult(
                PillBoxIdentificationSource.Rfid,
                uid,
                patientId.Trim()));
        }

        public void HandleRfidChanged(string oldUid, string newUid)
        {
            if (!IsActive)
            {
                return;
            }

            // A direct UID change is a physical replacement even when the firmware
            // did not emit an intermediate NO CARD. Never carry the old box's
            // verified identity into the new box.
            ClearVerification();
            RfidChanged?.Invoke(oldUid, newUid);
            HandleRfidPlaced(newUid);
        }

        public void HandleRfidRemoved(string uid)
        {
            if (!IsActive || !IsRfidPresent)
            {
                return;
            }

            IsRfidPresent = false;
            CurrentRfidUid = null;
            RfidRemoved?.Invoke(uid);
        }

        public void ClearVerification()
        {
            VerifiedResult = null;
            observedResult = null;
        }

        private void HandleIdentity(PillBoxIdentificationResult result)
        {
            if (string.IsNullOrWhiteSpace(result.PatientId))
            {
                return;
            }

            if (observedResult != null && !string.Equals(
                    observedResult.PatientId,
                    result.PatientId,
                    StringComparison.OrdinalIgnoreCase))
            {
                Conflict?.Invoke(observedResult, result);
                return;
            }

            observedResult = result;

            if (!IsAutoMode && !string.Equals(
                    result.PatientId,
                    expectedPatientId,
                    StringComparison.OrdinalIgnoreCase))
            {
                Mismatch?.Invoke(result, expectedPatientId);
                return;
            }

            if (VerifiedResult != null)
            {
                if (!string.Equals(
                        VerifiedResult.PatientId,
                        result.PatientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    Conflict?.Invoke(VerifiedResult, result);
                }
                return;
            }

            VerifiedResult = result;
            Verified?.Invoke(result);
        }
    }
}
