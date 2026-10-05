const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const SRC_DIR = './src';
const TESTS_DIR = './tests';
let violationsCount = 0;
const violationsLog = [];
function report(filePath, message) {
violationsCount++;
violationsLog.push(`[${violationsCount}] ${filePath}: ${message}`);
}
function stripLineComments(line) {
return line.replace(/\/\/.*$/, '');
}
function getMiddlewareContent() {
let middlewareContent = '';
function findFile(dir) {
if (!fs.existsSync(dir)) return;
const files = fs.readdirSync(dir);
for (const file of files) {
const fullPath = path.join(dir, file);
if (fs.statSync(fullPath).isDirectory()) {
findFile(fullPath);
} else if (file === 'ExceptionHandlingMiddleware.cs') {
middlewareContent = fs.readFileSync(fullPath, 'utf8');
}
}
}
findFile(SRC_DIR);
return middlewareContent;
}
const args = process.argv.slice(2);
const baseIdx = args.indexOf('--base');
let baseBranch = (baseIdx !== -1 && args[baseIdx + 1]) ? args[baseIdx + 1] : null;
if (!baseBranch) {
console.error('Ошибка: Не указана базовая ветка. Запустите скрипт с флагом: node check.js --base clean-base');
process.exit(1);
}
console.log(`Началась проверка конвенций в режиме DIFF против базы: ${baseBranch}...\n`);
let diffFiles = {};
let readmeChanged = false;
try {
const diffNames = execSync(`git diff --name-only ${baseBranch}...HEAD`, { encoding: 'utf8' });
if (diffNames.split(/\r?\n/).includes('README.md')) {
readmeChanged = true;
}
const diffOutput = execSync(`git diff ${baseBranch}...HEAD`, { encoding: 'utf8' });
const lines = diffOutput.split(/\r?\n/);
let currentFile = null;
for (let line of lines) {
if (line.startsWith('diff --git ')) {
const match = line.match(/b\/(.+)$/);
if (match) {
currentFile = match[1];
diffFiles[currentFile] = {
addedLines: [],
isNew: false
};
}
} else if (line.startsWith('new file mode ')) {
if (currentFile && diffFiles[currentFile]) {
diffFiles[currentFile].isNew = true;
}
} else if (line.startsWith('+') && !line.startsWith('+++')) {
if (currentFile && diffFiles[currentFile]) {
diffFiles[currentFile].addedLines.push(line.substring(1));
}
}
}
} catch (err) {
console.error('Ошибка при выполнении команд git. Убедитесь, что ветка существует и проект является git-репозиторием.', err.message);
process.exit(1);
}
const middlewareContent = getMiddlewareContent();
let hasAddedHttpEndpoint = false;
for (const [filePath, fileData] of Object.entries(diffFiles)) {
const normPath = filePath.replace(/\\/g, '/');
if (!normPath.endsWith('.cs')) continue;
if (fileData.isNew && normPath.includes('src/CarRental.Domain/Exceptions/')) {
const className = path.basename(normPath, '.cs');
if (middlewareContent && !middlewareContent.includes(className)) {
report(filePath, `Новое исключение '${className}' не найдено/не замаплено в ExceptionHandlingMiddleware.cs`);
}
}
fileData.addedLines.forEach(rawLine => {
const line = stripLineComments(rawLine);
if (normPath.includes('src/CarRental.Api/Controllers/') && /catch\s*\(/.test(line)) {
report(filePath, `Запрещен try/catch в контроллере на строке: "${rawLine.trim()}"`);
}
if ((normPath.includes('src/CarRental.Domain/') || normPath.includes('src/CarRental.Application/')) &&
/throw\s+new\s+(InvalidOperationException|ArgumentException)\b/.test(line)) {
report(filePath, `Запрещено генерировать базовое исключение: "${rawLine.trim()}". Используйте кастомные доменные исключения`);
}
if (normPath.includes('src/CarRental.Api/Controllers/') && /\[Http(Get|Post|Put|Patch|Delete)/.test(line)) {
hasAddedHttpEndpoint = true;
}
if (normPath.includes('src/CarRental.Domain/')) {
if (/using\s+CarRental\.Application\b/.test(line)) report(filePath, 'Domain не должен ссылаться на Application');
if (/using\s+CarRental\.Infrastructure\b/.test(line)) report(filePath, 'Domain не должен ссылаться на Infrastructure');
if (/using\s+CarRental\.Api\b/.test(line)) report(filePath, 'Domain не должен ссылаться на Api');
}
if (normPath.includes('src/CarRental.Application/')) {
if (/using\s+CarRental\.Infrastructure\b/.test(line)) report(filePath, 'Application не должен ссылаться на Infrastructure');
if (/using\s+CarRental\.Api\b/.test(line)) report(filePath, 'Application не должен ссылаться на Api');
if (/ApplicationDbContext\b/.test(line)) report(filePath, 'Application не должен использовать ApplicationDbContext напрямую');
if (/using\s+Microsoft\.EntityFrameworkCore\b/.test(line)) report(filePath, 'Application не должен напрямую использовать Microsoft.EntityFrameworkCore');
}
if (normPath.includes('src/CarRental.Api/Controllers/')) {
if (/ApplicationDbContext\b/.test(line)) report(filePath, 'Контроллер не должен инжектить ApplicationDbContext');
if (/new\s+\w+Repository\s*\(/.test(line)) report(filePath, 'Контроллер не должен создавать экземпляры репозиториев (new Repository)');
if (/using\s+CarRental\.Domain\.Entities\b/.test(line) || /CarRental\.Domain\.Entities\./.test(line)) {
report(filePath, 'Контроллер не должен использовать Domain.Entities напрямую (используйте DTO)');
}
if (/catch\s*\(\s*Exception\s*\)/.test(line)) report(filePath, 'Запрещен общий catch (Exception) в контроллере');
if (/throw\s+new\s+Exception\b/.test(line)) report(filePath, 'Используйте кастомные исключения, а не throw new Exception()');
if (/using\s+Microsoft\.EntityFrameworkCore\b/.test(line)) report(filePath, 'Контроллер не должен импортировать Microsoft.EntityFrameworkCore');
}
if (normPath.includes('src/CarRental.Application/') || normPath.includes('src/CarRental.Infrastructure/')) {
const methodRegex = /(?:public|protected|private|internal|async|\s)\s+(?:virtual|override|abstract\s+)?Task(<[^>]+>)?\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)/g;
let match;
while ((match = methodRegex.exec(line)) !== null) {
const methodName = match[2];
const methodParams = match[3];
if (['where', 'class', 'struct', 'interface'].includes(methodName)) continue;
if (!methodName.endsWith('Async') && !['Task', 'Task.FromResult'].includes(methodName)) {
report(filePath, `Асинхронный метод '${methodName}' должен иметь суффикс 'Async'`);
}
if (!methodParams.includes('CancellationToken')) {
report(filePath, `Метод '${methodName}' возвращает Task, но в его параметрах отсутствует CancellationToken`);
}
}
}
if (/throw\s+new\s+Exception\b/.test(line)) {
report(filePath, 'Запрещено использование throw new Exception(...) для бизнес-логики');
}
});
if (normPath.includes('src/CarRental.Application/')) {
const fileContent = fs.existsSync(filePath) ? fs.readFileSync(filePath, 'utf8') : '';
const cleanContent = fileContent.replace(/\/\*[\s\S]*?\*\/|\/\/.*/gm, '');
const hasWriteInDiff = fileData.addedLines.some(l => /\.(Add|Update|Delete|Remove|AddAsync|UpdateAsync|RemoveAsync)\s*\(/.test(l));
if (hasWriteInDiff && !/SaveChangesAsync/.test(cleanContent) && !/interface\s+\w+/.test(cleanContent)) {
report(filePath, 'В файле добавлены мутирующие операции (Add/Update/Delete), но не найден вызов SaveChangesAsync');
}
}
}
if (hasAddedHttpEndpoint && !readmeChanged) {
report('README.md', 'В контроллерах объявлен новый HTTP-эндпоинт [Http*], но файл README.md не был обновлен в текущем коммите/диффе');
}
if (violationsCount > 0) {
console.log(`НАЙДЕНО НАРУШЕНИЙ КОНВЕНЦИЙ: ${violationsCount}\n`);
violationsLog.forEach(log => console.log(log));
process.exit(1);
} else {
console.log('Нарушений архитектурных конвенций не обнаружено');
process.exit(0);
}