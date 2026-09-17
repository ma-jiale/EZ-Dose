namespace DA_Assets.DM
{
    public enum DependencyManagerLocKey
    {
        log_script_change_detected,
        log_manual_check_started,
        log_no_status_change,
        log_dependency_items_found,
        log_changes_detected,
        log_dependencies_up_to_date,
        log_processing_check_type,
        log_processing_type_found,
        log_processing_type_not_found,
        log_manual_removal_protection,
        log_status_changed,
        log_final_desired_defines,
        log_final_defines_named_target,
        log_final_defines_group
    }

    public static class DependencyManagerLocExtensions
    {
        public static string Localize(this DependencyManagerLocKey key, params object[] args) =>
            DependencyManagerConfig.Instance.Localizator.GetLocalizedText(key, null, args);
    }
}
