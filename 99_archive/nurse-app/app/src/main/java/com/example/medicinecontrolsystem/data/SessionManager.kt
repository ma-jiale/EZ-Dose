package com.example.medicinecontrolsystem.data

import android.content.Context
import android.content.SharedPreferences

// 定义一个简单的数据类来封装登录信息
data class LoggedInUser(val id: Int, val role: UserRole, val name: String)

class SessionManager(context: Context) {

    private var prefs: SharedPreferences =
        context.getSharedPreferences("AppSession", Context.MODE_PRIVATE)

    companion object {
        const val USER_ID = "user_id"
        const val USER_ROLE = "user_role"
        const val USER_NAME = "user_name"
    }

    /**
     * 保存用户会话信息
     */
    fun saveSession(user: LoggedInUser) {
        val editor = prefs.edit()
        editor.putInt(USER_ID, user.id)
        // 我们需要将枚举角色转换为字符串来存储
        editor.putString(USER_ROLE, user.role.name)
        editor.putString(USER_NAME, user.name)
        editor.apply()
    }

    /**
     * 获取已保存的用户会话信息
     * @return 如果有保存的用户信息，则返回LoggedInUser对象；否则返回null。
     */
    fun getSession(): LoggedInUser? {
        val userId = prefs.getInt(USER_ID, -1)
        val userRoleString = prefs.getString(USER_ROLE, null)
        val userName = prefs.getString(USER_NAME, null)

        // 只有当ID和角色都有效时，才认为存在会话
        if (userId != -1 && userRoleString != null && userName != null) {
            return try {
                val userRole = UserRole.valueOf(userRoleString)
                LoggedInUser(userId, userRole, userName)
            } catch (e: IllegalArgumentException) {
                // 如果存储的角色字符串无效，则返回null
                null
            }
        }
        return null
    }

    /**
     * 清除用户会话信息（用于退出登录）
     */
    fun clearSession() {
        val editor = prefs.edit()
        editor.clear()
        editor.apply()
    }
}
