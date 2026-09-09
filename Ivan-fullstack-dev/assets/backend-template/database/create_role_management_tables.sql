-- 角色管理相关表（MSSQL）
-- 执行前请确保数据库已存在，根据需要切换 MSSQL / MySQL / SQLite 语法

-- 1. 角色表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,          -- 角色名称
        Code NVARCHAR(50) NOT NULL UNIQUE,   -- 角色编码（admin/user/editor）
        Description NVARCHAR(200),           -- 描述
        CreateTime DATETIME                  -- 创建时间
    );

    -- 默认角色数据
    INSERT INTO Roles (Name, Code, Description, CreateTime) VALUES (N'管理员', 'admin', N'系统管理员，拥有所有权限', GETDATE());
    INSERT INTO Roles (Name, Code, Description, CreateTime) VALUES (N'普通用户', 'user', N'普通用户', GETDATE());
END;

-- 2. 菜单表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Menus')
BEGIN
    CREATE TABLE Menus (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,          -- 菜单名称
        Path NVARCHAR(200),                  -- 路由路径（如 /users、/roles）
        Icon NVARCHAR(50),                   -- 图标（Element Plus 图标名）
        ParentId INT NULL,                   -- 父菜单ID（NULL=一级菜单）
        Sort INT DEFAULT 0,                  -- 排序号
        IsVisible BIT DEFAULT 1,             -- 是否可见
        CreateTime DATETIME
    );

    -- 默认菜单数据
    INSERT INTO Menus (Name, Path, Icon, ParentId, Sort, IsVisible, CreateTime) VALUES (N'首页', '/home', 'HomeFilled', NULL, 1, 1, GETDATE());
    INSERT INTO Menus (Name, Path, Icon, ParentId, Sort, IsVisible, CreateTime) VALUES (N'系统管理', NULL, 'Setting', NULL, 2, 1, GETDATE());
    INSERT INTO Menus (Name, Path, Icon, ParentId, Sort, IsVisible, CreateTime) VALUES (N'用户管理', '/users', 'User', 2, 1, 1, GETDATE());
    INSERT INTO Menus (Name, Path, Icon, ParentId, Sort, IsVisible, CreateTime) VALUES (N'角色管理', '/roles', 'UserFilled', 2, 2, 1, GETDATE());
    INSERT INTO Menus (Name, Path, Icon, ParentId, Sort, IsVisible, CreateTime) VALUES (N'菜单管理', '/menus', 'Menu', 2, 3, 1, GETDATE());
END;

-- 3. 角色菜单关联表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleMenus')
BEGIN
    CREATE TABLE RoleMenus (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleId INT NOT NULL,                 -- 角色ID
        MenuId INT NOT NULL                  -- 菜单ID
    );

    -- 管理员拥有所有菜单（MenuId 1-5）
    INSERT INTO RoleMenus (RoleId, MenuId) VALUES (1, 1);
    INSERT INTO RoleMenus (RoleId, MenuId) VALUES (1, 2);
    INSERT INTO RoleMenus (RoleId, MenuId) VALUES (1, 3);
    INSERT INTO RoleMenus (RoleId, MenuId) VALUES (1, 4);
    INSERT INTO RoleMenus (RoleId, MenuId) VALUES (1, 5);

    -- 普通用户仅首页
    INSERT INTO RoleMenus (RoleId, MenuId) VALUES (2, 1);
END;

-- 4. 角色按钮权限表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleMenuButtons')
BEGIN
    CREATE TABLE RoleMenuButtons (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleId INT NOT NULL,                 -- 角色ID
        MenuId INT NOT NULL,                 -- 菜单ID
        ButtonKey NVARCHAR(50) NOT NULL      -- 按钮权限标识（add/edit/delete/export/import）
    );

    -- 管理员拥有全部按钮权限
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 3, 'add');    -- 用户管理-新增
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 3, 'edit');   -- 用户管理-编辑
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 3, 'delete'); -- 用户管理-删除
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 4, 'add');    -- 角色管理-新增
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 4, 'edit');   -- 角色管理-编辑
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 4, 'delete'); -- 角色管理-删除
    INSERT INTO RoleMenuButtons (RoleId, MenuId, ButtonKey) VALUES (1, 4, 'permission'); -- 角色管理-权限分配
END;
