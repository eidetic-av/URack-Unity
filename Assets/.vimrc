" Loading plugins
call plug#begin()
Plug 'lervag/vimtex', { 'for': 'tex' }
Plug 'Valloric/YouCompleteMe', { 'do': './install.py' }
Plug 'rhysd/vim-clang-format'
Plug 'kana/vim-operator-user'
Plug 'scrooloose/nerdtree'
Plug 'SirVer/ultisnips'
Plug 'honza/vim-snippets'
Plug 'easymotion/vim-easymotion'
Plug 'ctrlpvim/ctrlp.vim'
Plug 'kana/vim-submode'
Plug 'nathanaelkane/vim-indent-guides'
Plug 'frazrepo/vim-rainbow'
Plug 'tpope/vim-surround'
Plug 'tpope/vim-commentary'
Plug 'tomasr/molokai'
call plug#end()

" load project specific .vimrc
set exrc

let maplocalleader = "\\"

" Set clipboard default to plus register
" this allows the same clipboard as the OS
set clipboard=unnamedplus

" Highlighting / colors
syntax on
set incsearch
let g:rainbow_active = 1
colorscheme molokai
let g:molokai_original = 1


" numbers gutter
highlight LineNr term=bold cterm=NONE ctermfg=DarkGrey ctermbg=NONE gui=NONE guifg=DarkGrey guibg=NONE

" tabs
highlight TabLineFill ctermfg=DarkGrey
highlight TabLine ctermbg=DarkGrey

set laststatus=0

" line numbering
set relativenumber
set number
" Spell checking
set spelllang=en_au
set spellfile=~/.vim/spell/en.utf-8.add
" Don't spell check single characters
:syn match SingleChar '\<\A*\a\A*\>' contains=@NoSpell
" Specific filetypes for enabling spell checker
autocmd FileType tex setlocal spell

" Indentation
filetype plugin indent on
" Indents word-wrapped lines as much as the 'parent' line
set breakindent
" Ensures word-wrap does not split words
set formatoptions=l
set lbr

" case insensitive search by default
set ignorecase
set smartcase

" ensures that moving up and down can be done within broken lines
nnoremap <expr> j v:count ? (v:count > 5 ? "m'" . v:count : '') . 'j' : 'gj'
nnoremap <expr> k v:count ? (v:count > 5 ? "m'" . v:count : '') . 'k' : 'gk'
nmap <Down> gj
nmap <Up> gk
vmap <Down> gj
vmap <Up> gk
imap <Down> <C-o>gj
imap <Up> <C-o>gk

" move by word with h and l
nmap <C-h> <C-Left>
nmap <C-l> <C-Right>
imap <C-h> <C-Left>
imap <C-l> <C-Right>
imap <C-j> <C-o>gj
imap <C-k> <C-o>gk

" Tab indent options
set tabstop=4
set softtabstop=4
set shiftwidth=4
set noexpandtab
set autoindent
set smartindent

" switch tabs and splits easier
nnoremap t gt
nnoremap T gT
nnoremap <C-w> <C-w>w

" keycode timeout delay
set timeoutlen=1000
set ttimeoutlen=5

" CtrlP settings
" open in a new tab by default
let g:ctrlp_prompt_mappings = {
    \ 'AcceptSelection("e")': ['<c-t>'],
    \ 'AcceptSelection("t")': ['<cr>', '<2-LeftMouse>'],
    \ }

" Easymotion settings
nmap f <Plug>(easymotion-bd-w)

" clangd auto completer
let g:ycm_clangd_binary_path = "/usr/bin/clangd"

" Snippets
let g:UltiSnipsExpandTrigger = "<c-j>"

" NERDTree
nnoremap <C-t> :NERDTreeFind<CR> 
let NERDTreeQuitOnOpen = 1
let NERDTreeMinimalUI = 1
let NERDTreeDirArrows = 1

" ----------------
" |  C++ config  |
" ----------------
let g:clang_format#code_style = "google"
set cino=N-s

" on save, use clangformat, then vim auto indenting
autocmd BufWritePre *.cpp :ClangFormat
autocmd BufWritePre *.cpp :normal gg=G''

" ---------------
" |  Tex config |
" ---------------
let g:tex_flavor='latex'
let g:vimtex_view_method = 'zathura'
let g:vimtex_latexmk_options = '-shell-escape'
let g:vimtex_quickfix_mode = 2
let g:vimtex_quickfix_open_on_warning = 0

