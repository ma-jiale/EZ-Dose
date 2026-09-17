using System;

namespace DA_Assets.FCU.Model
{
    [Flags]
    public enum FigmaScope
    {
        /// <summary>
        /// Read your name, email, and profile image.
        /// </summary>
        CurrentUserRead = 1 << 0,
        /// <summary>
        /// Read the contents of files, such as nodes and the editor type.
        /// </summary>
        FileContentRead = 1 << 1,
        /// <summary>
        /// Read published components and styles of files.
        /// </summary>
        LibraryContentRead = 1 << 2,
        /// <summary>
        /// Read your design system analytics. Note: Enterprise plan only.
        /// </summary>
        LibraryAnalyticsRead = 1 << 3,
        /// <summary>
        /// Read data of individual published components and styles.
        /// </summary>
        LibraryAssetsRead = 1 << 4,
        /// <summary>
        /// Read organization activity logs. Note: Enterprise plan only. Must be an organization admin.
        /// </summary>
        OrgActivityLogRead = 1 << 5,
        /// <summary>
        /// Read text event data in the organization. Note: Enterprise plans with Governance+ only. Must be an organization admin.
        /// </summary>
        OrgDiscoveryRead = 1 << 6,
        /// <summary>
        /// List projects and files in projects.
        /// </summary>
        ProjectsRead = 1 << 7,
        /// <summary>
        /// Read most recent selection in files you can access.
        /// </summary>
        SelectionsRead = 1 << 8,
        /// <summary>
        /// Read published components and styles of teams.
        /// </summary>
        TeamLibraryContentRead = 1 << 9,
        /// <summary>
        /// Read metadata of webhooks.
        /// </summary>
        WebhooksRead = 1 << 10,
        /// <summary>
        /// Create and manage webhooks.
        /// </summary>
        WebhooksWrite = 1 << 11,
        /// <summary>
        /// Read the comments for files.
        /// </summary>
        FileCommentsRead = 1 << 12,
        /// <summary>
        /// Post and delete comments and comment reactions in files.
        /// </summary>
        FileCommentsWrite = 1 << 13,
        /// <summary>
        /// Read dev resources in files.
        /// </summary>
        FileDevResourcesRead = 1 << 14,
        /// <summary>
        /// Write dev resources to files.
        /// </summary>
        FileDevResourcesWrite = 1 << 15,
        /// <summary>
        /// Read metadata of files.
        /// </summary>
        FileMetadataRead = 1 << 16,
        /// <summary>
        /// Read variables in files. Note: Enterprise plan only.
        /// </summary>
        FileVariablesRead = 1 << 17,
        /// <summary>
        /// Write variables and collections in files. Note: Enterprise plan only.
        /// </summary>
        FileVariablesWrite = 1 << 18,
        /// <summary>
        /// Read the version history for files you can access.
        /// </summary>
        FileVersionsRead = 1 << 19
    }
}