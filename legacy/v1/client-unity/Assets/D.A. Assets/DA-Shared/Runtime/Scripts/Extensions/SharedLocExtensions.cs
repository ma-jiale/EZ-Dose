using DA_Assets.Singleton;

namespace DA_Assets.Shared.Extensions
{
    public static class SharedLocExtensions
    {
        public static string Localize(this SharedLocKey key, params object[] args) =>
            SharedConfig.Instance.Localizator.GetLocalizedText(key, null, args);

        public static string Localize(this SharedLocKey key, DALanguage lang, params object[] args) =>
            SharedConfig.Instance.Localizator.GetLocalizedText(key, lang, args);
    }

    public enum SharedLocKey
    {
        label_stable_version,
        label_buggy_version,
        label_beta_version,
        label_stable_version_emoji,
        label_buggy_version_emoji,
        label_beta_version_emoji,
        label_latest_version,
        label_your_version,
        label_made_by,
        log_request_sender_load_image_error,
        log_request_sender_get_error,
        log_request_sender_post_error,
        log_openai_request_started,
        log_openai_empty_response,
        log_openai_invalid_response,
        log_initial_language_set,
        log_localization_format_error,
        log_scene_backup_created,
        log_scene_backup_failed,
        log_cant_execute_no_backup,
        log_selection_null,
        log_object_not_selected,
        log_prefab_reset,
        log_no_components,
        log_properties_reset,
        log_canvas_children_destroyed,
        log_gameobject_is_null,
        log_select_folder_inside_assets,
        log_sprites_removed,
        log_sprite_provider_not_found,
        log_sprite_border_updated,
        log_sprite_reimport_success,
        log_sprite_reimport_failed,
        log_scenehierarchy_method_missing,
        log_scenehierarchy_field_missing,
        log_urihelpers_type_not_found,
        log_makeasseturi_method_missing,
        log_null_datatable,
        log_unknown_group_type,
        log_update_checker_parse_failed,
        log_update_checker_load_failed,
        text_close,
        label_help
    }
}
