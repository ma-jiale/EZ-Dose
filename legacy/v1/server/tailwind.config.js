/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "D:/WorkSpace/Dispenser/EZ-Dose-server/templates/**/*.html",
    "D:/WorkSpace/Dispenser/EZ-Dose-server/templates/*.html",
    "D:/WorkSpace/Dispenser/EZ-Dose-server/static/js/**/*.js",
    "./node_modules/flowbite/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        // ===== A. 品牌色：暖黏土（参照 Claude），仅用于品牌外壳层 =====
        brand: {
          50:  "#FBF3EF",
          100: "#F6E6DC",
          200: "#ECCAB6",
          300: "#DFA98C",
          400: "#D08560",
          500: "#C2603C", // 品牌主色：logo、主按钮、链接、选中态
          600: "#A94F2F", // hover
          700: "#8A3F26", // active
          800: "#6E331F",
          900: "#4E2417",
        },

        // ===== 画布与中性色：暖灰 stone，营造米色质感 =====
        canvas:  "#FAF9F5",   // 页面主背景（米色画布）
        surface: "#FFFFFF",   // 卡片 / 表格背景
        sunken:  "#F4F2EC",   // 表头、次级填充、侧栏
        line:    "#E9E5DC",   // 边框 / 分隔线（暖调）
        stone: {
          400: "#9A968D",      // 辅助说明
          500: "#6B6862",      // 次要文字
          700: "#3A3833",      // 正文
          900: "#1F1E1C",      // 标题（近黑，呼应 OpenAI 的克制）
        },

        // ===== C. 功能状态色：数据 / 操作 / 设备通用，高对比、对色盲友好 =====
        status: {
          success: { 50: "#ECFDF3", 600: "#15803D" }, // 成功 / 已完成 / 已连接
          info:    { 50: "#EFF4FF", 600: "#1D4ED8" }, // 进行中 / 待处理 / 打印中
          warning: { 50: "#FFFBEB", 600: "#B45309" }, // 待处理需关注 / 未标定 / 无打印机
          danger:  { 50: "#FEF2F2", 600: "#B91C1C" }, // 失败 / 错误 / 异常
        },

        // ===== B. 药品安全专用色：专色专用，绝不他用 =====
        safety: {
          alert:   { 50: "#FDF2F8", 600: "#9D174D" }, // 高警示药品（玫红）
          allergy: { 50: "#FEF2F2", 600: "#B42318" }, // 过敏（深红）
        },
      },
      fontFamily: {
        // 正文与所有 UI 控件：人文无衬线
        sans:  ["Inter", "system-ui", "-apple-system", "Segoe UI", "PingFang SC", "Microsoft YaHei", "sans-serif"],
        // 仅页面级大标题：衬线
        serif: ["Georgia", "Times New Roman", "Songti SC", "serif"],
      },
    },
  },
  plugins: [
    require('flowbite/plugin')
  ],
}
