"""
GUI 界面模块
使用 tkinter 实现数据库配置、表选择、字段预览的交互界面。
只负责收集元数据（表名、字段信息），不生成代码。
"""

import json
import os
import tkinter as tk
from tkinter import ttk, messagebox
import threading
import queue
from typing import Any

from db_connector import connect_db, get_tables, get_columns
from type_mapping import map_db_type_to_csharp

# 历史记录文件路径（与 gui.py 同目录）
_HISTORY_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".conn_history.json")
_MAX_HISTORY = 5


def _load_history() -> list[str]:
    """从文件加载连接串历史记录"""
    try:
        if os.path.exists(_HISTORY_FILE):
            with open(_HISTORY_FILE, "r", encoding="utf-8") as f:
                data = json.load(f)
                if isinstance(data, list):
                    return [s for s in data if isinstance(s, str) and s.strip()]
    except Exception:
        pass
    return []


def _save_history(history: list[str]):
    """保存连接串历史记录到文件"""
    try:
        with open(_HISTORY_FILE, "w", encoding="utf-8") as f:
            json.dump(history, f, ensure_ascii=False, indent=2)
    except Exception:
        pass


def _add_to_history(conn_str: str, existing: list[str]) -> list[str]:
    """将新连接串加入历史，去重并保持最近 5 条，最新的在前"""
    s = conn_str.strip()
    if not s:
        return existing
    # 去重：如果已存在则移除旧位置
    filtered = [item for item in existing if item != s]
    # 插入到最前面
    filtered.insert(0, s)
    # 只保留最近 _MAX_HISTORY 条
    return filtered[:_MAX_HISTORY]


class DbModelGenGUI:
    """数据库元数据收集 GUI"""

    def __init__(self, result_queue: queue.Queue):
        self.result_queue = result_queue
        self.conn = None
        self.db_type = ""
        self.tables: list[str] = []
        self.columns_cache: dict[str, list[dict]] = {}

        # 加载连接串历史记录
        self._conn_history: list[str] = _load_history()

        self.root = tk.Tk()
        self.root.title("数据库表结构浏览器 - 选择表和字段")
        self.root.geometry("900x600")
        self.root.resizable(True, True)

        style = ttk.Style()
        style.theme_use("clam")

        self._build_ui()
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    def _build_ui(self):
        """构建 UI 界面"""
        # ── 顶部：数据库连接配置 ──
        config_frame = ttk.LabelFrame(self.root, text="数据库连接配置", padding=10)
        config_frame.pack(fill=tk.X, padx=10, pady=(10, 5))

        row1 = ttk.Frame(config_frame)
        row1.pack(fill=tk.X, pady=2)

        ttk.Label(row1, text="数据库类型:", width=12).pack(side=tk.LEFT)
        self.db_type_var = tk.StringVar(value="mssql")
        db_type_combo = ttk.Combobox(
            row1, textvariable=self.db_type_var,
            values=["mssql", "mysql", "sqlite"],
            state="readonly", width=12
        )
        db_type_combo.pack(side=tk.LEFT, padx=(0, 10))

        ttk.Label(row1, text="连接字符串:", width=12).pack(side=tk.LEFT)
        self.conn_str_var = tk.StringVar()
        self.conn_str_combo = ttk.Combobox(
            row1, textvariable=self.conn_str_var,
            values=self._conn_history,
            width=58
        )
        self.conn_str_combo.pack(side=tk.LEFT, fill=tk.X, expand=True)
        # 绑定输入事件：用户手动输入或选择后自动更新下拉列表
        self.conn_str_var.trace_add("write", self._on_conn_str_changed)

        row2 = ttk.Frame(config_frame)
        row2.pack(fill=tk.X, pady=(5, 0))

        self.connect_btn = ttk.Button(row2, text="连接数据库", command=self._connect_db)
        self.connect_btn.pack(side=tk.LEFT)

        self.status_var = tk.StringVar(value="未连接")
        ttk.Label(row2, textvariable=self.status_var, foreground="gray").pack(
            side=tk.LEFT, padx=(10, 0)
        )

        # ── 中部：表列表 + 字段预览 ──
        middle_frame = ttk.Frame(self.root)
        middle_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        # 左侧：表列表
        table_frame = ttk.LabelFrame(middle_frame, text="数据库表（多选）", padding=5)
        table_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=False, padx=(0, 5))

        self.table_listbox = tk.Listbox(
            table_frame, selectmode=tk.MULTIPLE,
            width=30, exportselection=False
        )
        self.table_listbox.pack(fill=tk.BOTH, expand=True)
        self.table_listbox.bind("<<ListboxSelect>>", self._on_table_select)

        # 右侧：字段预览
        field_frame = ttk.LabelFrame(middle_frame, text="字段预览（选中表后显示）", padding=5)
        field_frame.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True)

        columns = ("字段名", "数据库类型", "可空", "C# 类型", "注释")
        self.field_tree = ttk.Treeview(
            field_frame, columns=columns,
            show="headings", selectmode=tk.EXTENDED
        )
        for col in columns:
            self.field_tree.heading(col, text=col)
            self.field_tree.column(col, width=100 if col != "注释" else 200)

        field_scroll = ttk.Scrollbar(field_frame, orient=tk.VERTICAL, command=self.field_tree.yview)
        self.field_tree.configure(yscrollcommand=field_scroll.set)

        self.field_tree.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        field_scroll.pack(side=tk.RIGHT, fill=tk.Y)

        # ── 底部：确认按钮 ──
        bottom_frame = ttk.Frame(self.root)
        bottom_frame.pack(fill=tk.X, padx=10, pady=(5, 10))

        ttk.Label(
            bottom_frame,
            text="提示：选中表后点击「确认选择」，将返回所选表的字段元数据给调用方",
            foreground="gray"
        ).pack(side=tk.LEFT)

        self.confirm_btn = ttk.Button(
            bottom_frame, text="确认选择，返回元数据",
            command=self._confirm, state=tk.DISABLED
        )
        self.confirm_btn.pack(side=tk.RIGHT)

    def _on_conn_str_changed(self, *_args):
        """连接字符串输入变化时，实时更新下拉列表（去重 + 最新在前）"""
        current = self.conn_str_var.get().strip()
        if not current:
            return
        # 更新内存中的历史记录
        self._conn_history = _add_to_history(current, self._conn_history)
        # 更新 Combobox 下拉值
        self.conn_str_combo["values"] = self._conn_history

    def _connect_db(self):
        """连接数据库"""
        db_type = self.db_type_var.get()
        conn_str = self.conn_str_var.get().strip()

        if not conn_str:
            messagebox.showerror("错误", "请输入连接字符串")
            return

        # 持久化保存历史记录
        self._conn_history = _add_to_history(conn_str, self._conn_history)
        _save_history(self._conn_history)
        self.conn_str_combo["values"] = self._conn_history

        self.connect_btn.config(state=tk.DISABLED, text="连接中...")
        self.status_var.set("正在连接...")

        def do_connect():
            try:
                conn = connect_db(db_type, conn_str)
                tables = get_tables(db_type, conn)
                self.root.after(0, lambda: self._on_connected(db_type, conn, tables))
            except Exception as e:
                self.root.after(0, lambda: self._on_connect_error(str(e)))

        threading.Thread(target=do_connect, daemon=True).start()

    def _on_connected(self, db_type: str, conn, tables: list[str]):
        """连接成功回调"""
        self.conn = conn
        self.db_type = db_type
        self.tables = tables
        self.columns_cache = {}

        self.table_listbox.delete(0, tk.END)
        for t in tables:
            self.table_listbox.insert(tk.END, t)

        for item in self.field_tree.get_children():
            self.field_tree.delete(item)

        self.connect_btn.config(state=tk.NORMAL, text="连接数据库")
        self.status_var.set(f"已连接 - {len(tables)} 个表")
        self.confirm_btn.config(state=tk.NORMAL)

    def _on_connect_error(self, error: str):
        """连接失败回调"""
        self.connect_btn.config(state=tk.NORMAL, text="连接数据库")
        self.status_var.set("连接失败")
        messagebox.showerror("连接失败", f"无法连接数据库:\n{error}")

    def _on_table_select(self, event=None):
        """表选中事件 - 加载字段预览"""
        selection = self.table_listbox.curselection()
        if not selection:
            return

        last_idx = selection[-1]
        table_name = self.tables[last_idx]

        for item in self.field_tree.get_children():
            self.field_tree.delete(item)

        if table_name not in self.columns_cache:
            try:
                columns = get_columns(self.db_type, self.conn, table_name)
                self.columns_cache[table_name] = columns
            except Exception as e:
                messagebox.showerror("错误", f"读取表 {table_name} 字段失败:\n{e}")
                return

        columns = self.columns_cache[table_name]
        for col in columns:
            csharp_type = map_db_type_to_csharp(col["db_type"], col["is_nullable"])
            self.field_tree.insert("", tk.END, values=(
                col["name"],
                col["db_type"],
                "YES" if col["is_nullable"] else "NO",
                csharp_type,
                col.get("description", ""),
            ))

    def _confirm(self):
        """确认选择，返回元数据"""
        selection = self.table_listbox.curselection()
        if not selection:
            messagebox.showwarning("提示", "请先选择要导出的表")
            return

        selected_tables = [self.tables[i] for i in selection]

        # 确保所有选中表的字段已缓存
        for table_name in selected_tables:
            if table_name not in self.columns_cache:
                try:
                    columns = get_columns(self.db_type, self.conn, table_name)
                    self.columns_cache[table_name] = columns
                except Exception as e:
                    messagebox.showerror("错误", f"读取表 {table_name} 失败:\n{e}")
                    return

        # 构建元数据结果
        result: dict[str, Any] = {
            "db_type": self.db_type,
            "tables": {},
        }

        for table_name in selected_tables:
            columns = self.columns_cache[table_name]
            result["tables"][table_name] = columns

        self.result_queue.put(result)
        self.root.destroy()

    def _on_close(self):
        """关闭窗口"""
        self.result_queue.put(None)
        self.root.destroy()

    def run(self):
        """启动 GUI 主循环"""
        self.root.mainloop()


def show_gui_and_get_result() -> dict | None:
    """
    弹出 GUI 界面，等待用户操作完成后返回元数据。

    Returns:
        dict: {
            "db_type": "mssql",
            "tables": {
                "TableName": [
                    {"name": "Id", "db_type": "int", "is_nullable": False, "description": ""},
                    ...
                ]
            }
        }
        None: 用户取消
    """
    result_queue: queue.Queue = queue.Queue()
    gui = DbModelGenGUI(result_queue)
    gui.run()
    return result_queue.get()
