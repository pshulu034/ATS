# git介绍
开源分布式版本控制系统，由Linus Torvalds于2005年创建
- 版本管理
- 协作开发

github:代码插管平台，开发者将代码提交到git并公开,其它人可以通过开源协议使用代码资源
gitlab/gittee:国内

cursor可以连接github的mcp,用自然语言操作github

## git与svn区别
svn: 工作副本 ↔ 遠端倉庫
git: 工作目錄 → 暫存區 → 本地倉庫 → 遠端

暫存區
	本身也是一个完整的快照，add指令不只是加入追踪，而是拍一张快照到暂存区.
	暫存區只有上一次暂存的快照，没有历史版本。
	暂存是以文件为单位的
	
本地倉庫:
	在离网的情况下，git也可以实现版本管理
	倉庫以分支为单位，不是以文件为单位
	倉庫有完整的版本记录，可以回退到任意版本

遠端倉庫:
	可以是GitHub和GitLab等服务，也可以是公司内部git服务
	只能pull到最新版本，如果回退到指定版本，需先fetch到本地后，再指定版本回退

## 安装
下载 https://git-scm.com/downloads
测试 `git --version`

## 架构
    服务端
        远程服务器 Git是GitHub和GitLab等服务的基础
        公司git服务器

    客户端
        git dash
        git bash
        git gui
        git addin

## 配置
1. 配置用户信息
这是您第一次使用 Git 时需要做的。这些信息将附加到您的提交中。
git config --global user.name "hulu9100"
git config --global user.email "pshulu034@gmail.com"
级别: 目录/全局/系统

2. 访问凭据
GitHub 从 2021 年 8 月起不再支持通过用户名和密码来进行 Git 操作
- HTTPS + 个人访问令牌（PAT） 
  - 生成 PAT 令牌      (git dash)Generate new token
  - 将 PAT 存储在本地  `git config --global credential.helper cache`
- SSH 密钥
  - 生成 SSH 密钥  (默认为 ~/.ssh/id_rsa.pub)
    `ssh-keygen -t rsa -b 4096 -C "pshulu034@gmail.com"`
    id_rsa     私钥
    id_rsa.pub 公钥
  - 将公钥添加到 GitHub
  登录 GitHub，进入 Settings > SSH and GPG keys，点击 New SSH key，将公钥粘贴进去。
  - 测试 SSH 连接
  `ssh -T git@github.com`

# 基本流程
工作区 -> (add) ->  暂存区 -> (commit) ->  本地仓库 -> (push) -> 远程仓库(GitHub)
区别: svn没有仓库的概念

文件的生命周期
- Untracked (未跟踪)：Git 还没有开始跟踪的新文件。
- Staged (已暂存)：文件已被标记，将在下一次提交中被保存。
- Modified (已修改)：文件已被更改，但尚未添加到暂存区。 哪怕是未add过的文件被修改也会显示
- Committed(已提交)

动作和状态
- new file(新建文件)     状态:Untracked

- stage(add)            动作:work区同步到index区    状态: Untracked/modified -> staged

- code                  状态: staged -> modified
  
- unmodified(撤消修改)   动作:index区同步到work区    状态: modified -> staged

- unstage (撤消暂存)     状态:staged -> Untracked
  
- commit                动作:index区同步至local区(分支)

- reset                 动作:回退到指定版本至工作区
  
- push                  动作:本地仓库同步至远程仓库
  
- pull                  动作:从远程仓库下载至工作区
  
## 基本操作指令
- 状态  git status

- 暂存(stage) 
git add [filename]
git add .

- 提交  
//这里的提交是指交到本地仓库, 而且提交的是整个分支
git commit -m "Your descriptive commit message"
修改commit描述   
git commit --amend -m "正確訊息"

- 远端操作
克隆   git clone [uri]
推送   git push origin [分支名稱]
拉取   git pull origin [分支名稱]   #(fetch + merge)   类比:update
覆蓋   git fetch                   #强制覆蓋本地

## 应用场景
###  远程建库
远程建库的场景更常见
```
git clone https://github.com/username/myproject.git
cd myproject

# 開始改程式碼...
code .                     # 用 VS Code 開啟

# 修改完後
git pull origin <分支名>       # 先拉最新版，避免衝突
git add .
git commit -m "feat: 新增登入功能"
git push
```
##  本機建库
```
# 1. 建立資料夾並進入
mkdir myproject
cd myproject

# 2. 初始化 git 倉庫,目录里会出现 .git 文件夹 → 表示已经是一个 Git 仓库
git init

# 3. 加入檔案
touch README.md
git add README.md          # 加入單一檔案
git add .                  # 加入目前資料夾所有變更

# 4. 建立第一次 commit
git commit -m "Initial commit"

# 5. 連到遠端（GitHub / GitLab / Gitee）
git remote add origin https://github.com/username/myproject.git

# 6. 推上遠端
git push origin <分支名>
```

# 日志和版本回退
## 日志
- 查看日志
git log
git log --oneline --graph --all  #樹狀歷史

- 查看修改  #查看暂存区和本地仓库之间的差异。
git diff  #查看工作区与暂存区的区别
git diff --staged  #查看工作区与本地仓库的区别

## 版本回退
- 撤消修改
git restore [filename]

- 取消暂存(unstage)
git restore --staged [filename]

- 回版回退
git reset --hard <hash>

- 重新push
git push -- force -u origin <branch>

## 临时保存  
git 所有分支共用一个目录. 切换分支前当前分支一般要commit或stash,否则可能把未提交的修改和临时文件带到不该带的地方去，污染其他分支。

git stash      保存当前开发进度
git stash pop  恢复代码

```
# 1. 你正在 feature/login 分支写代码，改了一半， 突然要紧急修复线上 bug，必须切到 main 分支
git stash push -m "登录页面半成品，待完成"

# 切分支、修 bug、提交、推上去……
git switch main
# …修复完后切回来
git switch feature/login

git stash pop        # 或 git stash apply（保留记录更安全）

```

# 协作开发
## 分支操作 
1. 查看分支  
   git branch
   git branch -a
2. 创建分支  git branch dev
3. 切换分支  git checkout dev    #git switch 分支名
4. 创建并切换分支  git checkout -b dev
5. 合并分支  
    git checkout main   #先切换到目标分支
    git merge --no-ff -m "desc" [分支名]
6. 刪除本地分支  git branch -d 分支名
7. 刪除遠端分支  git push origin --delete 分支名
   
## 冲突处理

# git插件
## visual stido code
GitLens

问题: commit速度慢
修改 settings.json,但并没有效果
```
"git.autorefresh": false,
"git.enableSmartCommit": false,
"git.suggestSmartCommit": false,
"git.confirmSync": false,
"git.detectSubmodules": false,
"git.showCommitInput": false
```
目前只能改用commit提交
## visual stido ide















学习记录
1. 注册 pshulu034@gmail.com，登录 github dashboard:  
2. 安装 git
    git bash
    git gui
3. 配置    用户和SSH
4. dashboard：
create a new repository：
- choose visibility
- add readme
- add .gitignore
- add license

create branch

5. 下载  git clone   #不能在explorer中删除
- .gitignore
- LICENSE
- README.md

6. 忽略追踪 .gitignore
  filename
  *.png

7. 基本流程
   本地分支-远程分支
   pull request 

=======================
廖雪峰git教程