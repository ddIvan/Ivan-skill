-- Ivan-fullstack-dev init script (unified permission tree v2)
USE [IvanDemo];
IF OBJECT_ID('dbo.MenuActionRelations','U') IS NOT NULL DROP TABLE dbo.MenuActionRelations; -- 旧模型表，新库不存在时跳过;
IF OBJECT_ID('dbo.MenuActions','U') IS NOT NULL DROP TABLE dbo.MenuActions;
IF OBJECT_ID('dbo.RoleMenuButtons','U') IS NOT NULL DROP TABLE dbo.RoleMenuButtons; -- 旧模型表，新库不存在时跳过
IF OBJECT_ID('dbo.RoleMenus','U') IS NOT NULL DROP TABLE dbo.RoleMenus;
IF OBJECT_ID('dbo.UserRoles','U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.Menus','U') IS NOT NULL DROP TABLE dbo.Menus;
IF OBJECT_ID('dbo.Roles','U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Users','U') IS NOT NULL DROP TABLE dbo.Users;
CREATE TABLE dbo.Users (Id INT IDENTITY(1,1) PRIMARY KEY, UserName NVARCHAR(50) NOT NULL, PasswordHash NVARCHAR(200) NOT NULL, DisplayName NVARCHAR(50) NULL, IsEnabled BIT NOT NULL DEFAULT(1), CreateTime DATETIME NOT NULL DEFAULT(GETDATE()));
CREATE TABLE dbo.Roles (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Code NVARCHAR(50) NOT NULL, Description NVARCHAR(200) NULL, CreateTime DATETIME NOT NULL DEFAULT(GETDATE()));
CREATE TABLE dbo.Menus (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(50) NOT NULL, MenuType TINYINT NOT NULL DEFAULT(2), Path NVARCHAR(200) NULL, Icon NVARCHAR(50) NULL, ParentId INT NULL, FullPath NVARCHAR(500) NULL, Level INT NOT NULL DEFAULT(0), Sort INT NOT NULL DEFAULT(0), IsVisible BIT NOT NULL DEFAULT(1), IsEnabled BIT NOT NULL DEFAULT(1), PermissionCode NVARCHAR(100) NULL, ControllerAction NVARCHAR(200) NULL, Remark NVARCHAR(200) NULL, CreateTime DATETIME NOT NULL DEFAULT(GETDATE()));
CREATE TABLE dbo.UserRoles (Id INT IDENTITY(1,1) PRIMARY KEY, UserId INT NOT NULL, RoleId INT NOT NULL, CreateTime DATETIME NOT NULL DEFAULT(GETDATE()));
CREATE TABLE dbo.RoleMenus (Id INT IDENTITY(1,1) PRIMARY KEY, RoleId INT NOT NULL, MenuId INT NOT NULL, CreateTime DATETIME NOT NULL DEFAULT(GETDATE()));
CREATE UNIQUE INDEX UX_Roles_Code ON dbo.Roles(Code); CREATE INDEX IX_Menus_ParentId ON dbo.Menus(ParentId); CREATE INDEX IX_Menus_FullPath ON dbo.Menus(FullPath); CREATE INDEX IX_Menus_PermissionCode ON dbo.Menus(PermissionCode); CREATE INDEX IX_UserRoles_UserId ON dbo.UserRoles(UserId); CREATE INDEX IX_RoleMenus_RoleId ON dbo.RoleMenus(RoleId);
INSERT INTO dbo.Roles (Name, Code, Description) VALUES (N'系统管理员', N'admin', N'超级管理员'), (N'普通用户', N'user', N'普通用户');
INSERT INTO dbo.Users (UserName, PasswordHash, DisplayName) VALUES (N'admin', N'AQIDBAUGBwgJCgsMDQ4PEA==.SJ34IaoONdCI30nVhP7h41dba31Qp+CGBYhy6bY72To=', N'系统管理员');
INSERT INTO dbo.UserRoles (UserId, RoleId) SELECT u.Id, r.Id FROM dbo.Users u CROSS JOIN dbo.Roles r WHERE u.UserName=N'admin' AND r.Code IN (N'admin', N'user');
-- 种子：菜单树（MenuType 1=目录 2=菜单 3=按钮；按钮节点 PermissionCode 对应 [RequirePerm]）；实际 Id 以 IDENTITY 顺序为准，下方变量用名称锚定
INSERT INTO dbo.Menus (Name, MenuType, Path, Icon, ParentId, FullPath, Level, Sort, IsVisible, IsEnabled) VALUES
(N'系统管理', 1, NULL, N'Setting', NULL, N'/', 0, 1, 1, 1);
DECLARE @root INT = SCOPE_IDENTITY(); UPDATE dbo.Menus SET FullPath = N'/' + CAST(@root AS NVARCHAR(20)) WHERE Id = @root;
INSERT INTO dbo.Menus (Name, MenuType, Path, Icon, ParentId, Level, Sort) VALUES (N'首页', 2, N'/home', N'HomeFilled', @root, 1, 1);
DECLARE @mUsers INT, @mRoles INT, @mMenus INT;
INSERT INTO dbo.Menus (Name, MenuType, Path, Icon, ParentId, Level, Sort) VALUES (N'用户管理', 2, N'/users', N'User', @root, 1, 2); SET @mUsers = SCOPE_IDENTITY();
INSERT INTO dbo.Menus (Name, MenuType, Path, Icon, ParentId, Level, Sort) VALUES (N'角色管理', 2, N'/roles', N'UserFilled', @root, 1, 3); SET @mRoles = SCOPE_IDENTITY();
INSERT INTO dbo.Menus (Name, MenuType, Path, Icon, ParentId, Level, Sort) VALUES (N'菜单管理', 2, N'/menus', N'Menu', @root, 1, 4); SET @mMenus = SCOPE_IDENTITY();
-- 用户管理按钮
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'查看用户', 3, @mUsers, 2, 0, N'users:view', N'UserController.GetPage');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'新增用户', 3, @mUsers, 2, 1, N'users:add', N'UserController.Create');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'编辑用户', 3, @mUsers, 2, 2, N'users:edit', N'UserController.Update');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'删除用户', 3, @mUsers, 2, 3, N'users:delete', N'UserController.Delete');
-- 角色管理按钮
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'查看角色', 3, @mRoles, 2, 0, N'roles:view', N'RolesController.GetPage');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'新增角色', 3, @mRoles, 2, 1, N'roles:add', N'RolesController.Create');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'编辑角色', 3, @mRoles, 2, 2, N'roles:edit', N'RolesController.Update');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'删除角色', 3, @mRoles, 2, 3, N'roles:delete', N'RolesController.Delete');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'分配权限', 3, @mRoles, 2, 4, N'roles:permission', N'RolesController.SaveRoleMenus');
-- 菜单管理按钮
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'查看菜单', 3, @mMenus, 2, 0, N'menus:view', N'MenusController.GetTree');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'新增菜单', 3, @mMenus, 2, 1, N'menus:add', N'MenusController.Create');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'编辑菜单', 3, @mMenus, 2, 2, N'menus:edit', N'MenusController.Update');
INSERT INTO dbo.Menus (Name, MenuType, ParentId, Level, Sort, PermissionCode, ControllerAction) VALUES (N'删除菜单', 3, @mMenus, 2, 3, N'menus:delete', N'MenusController.Delete');
-- FullPath 统一递归计算（父前缀 + / + Id）
;WITH T AS (SELECT Id, ParentId, CAST(N'/' + CAST(Id AS NVARCHAR(20)) AS NVARCHAR(500)) AS FullPath FROM dbo.Menus WHERE ParentId IS NULL
            UNION ALL SELECT c.Id, c.ParentId, CAST(p.FullPath + N'/' + CAST(c.Id AS NVARCHAR(20)) AS NVARCHAR(500)) FROM dbo.Menus c JOIN T p ON c.ParentId = p.Id)
UPDATE m SET m.FullPath = t.FullPath FROM dbo.Menus m JOIN T t ON m.Id = t.Id;
-- admin 角色授权全部菜单+按钮节点
INSERT INTO dbo.RoleMenus (RoleId, MenuId) SELECT (SELECT Id FROM dbo.Roles WHERE Code = N'admin'), Id FROM dbo.Menus;
-- 核对
SELECT N'菜单总数' AS Item, COUNT(*) AS Cnt FROM dbo.Menus UNION ALL SELECT N'按钮节点', COUNT(*) FROM dbo.Menus WHERE MenuType = 3 UNION ALL SELECT N'admin授权数', COUNT(*) FROM dbo.RoleMenus rm JOIN dbo.Roles r ON r.Id = rm.RoleId WHERE r.Code = N'admin';
PRINT N'数据库初始化完成（统一权限树模型 v2）';
