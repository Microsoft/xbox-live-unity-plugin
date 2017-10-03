﻿namespace Microsoft.Xbox.Services.Social.Manager
{
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Runtime.InteropServices;
    using Presence;

    public partial class SocialManagerPresenceRecord : ISocialManagerPresenceRecord, IEquatable<SocialManagerPresenceRecord>
    {

        internal SocialManagerPresenceRecord(IntPtr socialManagerPresenceRecordPtr)
        {
            this.m_socialManagerPresenceRecordPtr = socialManagerPresenceRecordPtr;

            SocialManager.SocialManagerPresenceRecord_c cPresenceRecord = Marshal.PtrToStructure<SocialManager.SocialManagerPresenceRecord_c>(socialManagerPresenceRecordPtr);
            UserState = cPresenceRecord.UserState;

            m_titleRecords = new List<SocialManagerPresenceTitleRecord>();
            if (cPresenceRecord.NumOfPresenceTitleRecords > 0)
            {
                IntPtr[] cTitleRecords = new IntPtr[cPresenceRecord.NumOfPresenceTitleRecords];
                Marshal.Copy(cPresenceRecord.PresenceTitleRecords, cTitleRecords, 0, cPresenceRecord.NumOfPresenceTitleRecords);
                foreach (IntPtr cTitleRecord in cTitleRecords)
                {
                    m_titleRecords.Add(new SocialManagerPresenceTitleRecord(cTitleRecord));
                }
            }
        }

        private delegate bool SocialManagerPresenceRecordIsUserPlayingTitle(IntPtr socialManagerPresenceRecord, uint titleId);
        public bool IsUserPlayingTitle(uint titleId)
        {
            return XboxLive.Instance.Invoke<bool, SocialManagerPresenceRecordIsUserPlayingTitle>(m_socialManagerPresenceRecordPtr, titleId);
        }

        public bool Equals(SocialManagerPresenceRecord other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            bool listsAreEqual = PresenceTitleRecords.Count == other.PresenceTitleRecords.Count;
            for (int i = 0; i < PresenceTitleRecords.Count && listsAreEqual; i++)
            {
                listsAreEqual = listsAreEqual && PresenceTitleRecords[i].Equals(other.PresenceTitleRecords[i]);
            }

            return listsAreEqual && this.UserState == other.UserState;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((SocialManagerPresenceRecord)obj);
        }
    }
}