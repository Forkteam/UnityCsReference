// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Experimental.Licensing;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class BasePackageVersion : IPackageVersion, ISerializationCallbackReceiver
    {
        private const string k_NoSubscriptionErrorMessage = "You do not have a subscription for this package";
        public string name => packageInfo?.name ?? string.Empty;

        [SerializeField]
        protected string m_DisplayName;
        public string displayName => m_DisplayName;

        [SerializeField]
        protected string m_Description;
        public string description => !string.IsNullOrEmpty(m_Description) ? m_Description : (packageInfo?.description ?? string.Empty);

        [SerializeField]
        protected string m_PackageUniqueId;
        public string packageUniqueId => m_PackageUniqueId;

        public virtual string authorLink => string.Empty;

        [SerializeField]
        protected string m_VersionString;
        protected SemVersion? m_Version;
        public SemVersion? version => m_Version;

        [SerializeField]
        protected long m_PublishedDateTicks;
        public DateTime? publishedDate => m_PublishedDateTicks == 0 ? packageInfo?.datePublished : new DateTime(m_PublishedDateTicks, DateTimeKind.Utc);

        [SerializeField]
        protected string m_PublishNotes;
        public string releaseNotes => m_PublishNotes;

        public DependencyInfo[] dependencies => packageInfo?.dependencies;
        public DependencyInfo[] resolvedDependencies => packageInfo?.resolvedDependencies;
        public EntitlementsInfo entitlements => packageInfo?.entitlements;

        public virtual RegistryInfo registry => null;

        [NonSerialized]
        private IPackage m_Package;
        public UI.IPackage package => m_Package;
        IPackage IPackageVersion.package
        {
            get => m_Package;
            set { m_Package = value; }
        }

        [SerializeField]
        protected PackageTag m_Tag;
        public bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) != 0;
        }

        public bool hasEntitlements => entitlements != null && (entitlements.licenseType != EntitlementLicenseType.Public || entitlements.status == EntitlementStatus.NotGranted || entitlements.status == EntitlementStatus.Granted);

        public bool hasEntitlementsError
        {
            get
            {
                if (!hasEntitlements || entitlements.isAllowed)
                    return packageInfo?.errors.Any(error =>
                        error.errorCode == ErrorCode.Forbidden ||
                        error.message.IndexOf(k_NoSubscriptionErrorMessage, StringComparison.InvariantCultureIgnoreCase) >= 0) ?? false;

                return true;
            }
        }

        public virtual PackageInfo packageInfo => null;
        public virtual IDictionary<string, string> categoryLinks => null;
        public virtual IEnumerable<UIError> errors => Enumerable.Empty<UIError>();
        public virtual IEnumerable<PackageSizeInfo> sizes => Enumerable.Empty<PackageSizeInfo>();
        public virtual IEnumerable<SemVersion> supportedVersions => Enumerable.Empty<SemVersion>();
        public virtual SemVersion? supportedVersion => null;

        public abstract string uniqueId { get; }
        public abstract string category { get; }
        public abstract string author { get; }
        public abstract bool isInstalled { get; }
        public abstract bool isFullyFetched { get; }
        public abstract bool isAvailableOnDisk { get; }
        public abstract bool isDirectDependency { get; }
        public abstract string localPath { get; }
        public abstract string versionString { get; }
        public abstract string versionId { get; }
        public virtual bool isUnityPackage => false;

        public bool IsDifferentVersionThanRequested
            => !string.IsNullOrEmpty(packageInfo?.projectDependenciesEntry) && !HasTag(PackageTag.Git | PackageTag.Local | PackageTag.Custom) &&
                packageInfo.projectDependenciesEntry != versionString;

        public bool IsRequestedButOverriddenVersion
            => !string.IsNullOrEmpty(versionString) && !isInstalled &&
                versionString == m_Package?.versions.primary.packageInfo?.projectDependenciesEntry;

        public virtual void OnBeforeSerialize()
        {
            // Do nothing
        }

        public virtual void OnAfterDeserialize()
        {
            SemVersionParser.TryParse(m_VersionString, out m_Version);
        }
    }
}
