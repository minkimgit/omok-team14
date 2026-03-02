const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const cors = require('cors');
const mongoose = require('mongoose');

const app = express();
const PORT = 3000;

// 1. 미들웨어 설정
app.use(cors());
app.use(express.json());

// 2. MongoDB 연결 설정
mongoose.connect('mongodb://127.0.0.1:27017/unity_game')
    .then(() => console.log("[DB] MongoDB 연결 성공"))
    .catch(err => console.error("[DB] MongoDB 연결 실패:", err));

// 3. 유저 데이터 스키마 정의
const userSchema = new mongoose.Schema({
    email: { type: String, required: true, unique: true },
    password: { type: String, required: true },
    createdAt: { type: Date, default: Date.now }
});

const User = mongoose.model('User', userSchema);

// 4. HTTP 서버 및 Socket.io 설정
const server = http.createServer(app);
const io = new Server(server, {
    cors: {
        origin: "*",
        methods: ["GET", "POST"]
    }
});

// 매칭 메이킹 대기열 및 방 정보
let matchmakingQueue = [];
let gameRooms = {};

// 5. 소켓 이벤트 처리
io.on('connection', (socket) => {
    console.log(`[알림] 새 클라이언트 접속! (ID: ${socket.id})`);

    // --- 회원가입 요청 처리 (복구 완료) ---
    socket.on('register', async (data) => {
        const { email, password } = data;
        console.log(`[회원가입 요청] ${email}`);

        if (!email || !password) {
            return socket.emit('registerResponse', {
                success: false,
                code: 2,
                message: "이메일/비밀번호 형식 오류"
            });
        }

        try {
            const existingUser = await User.findOne({ email });
            if (existingUser) {
                return socket.emit('registerResponse', {
                    success: false,
                    code: 1,
                    message: "이미 생성된 계정"
                });
            }

            const newUser = new User({ email, password });
            await newUser.save();

            socket.emit('registerResponse', {
                success: true,
                code: 0,
                message: "회원가입 완료"
            });
            console.log(`[DB] 새 유저 등록 성공: ${email}`);

        } catch (error) {
            console.error("[DB 에러]", error);
            socket.emit('registerResponse', {
                success: false,
                code: 99,
                message: "서버 내부 에러 발생"
            });
        }
    });

    // --- 로그인 요청 처리 ---
    socket.on('login', async (data) => {
        const { email, password } = data;
        console.log(`[로그인 요청] ${email}`);
        try {
            const user = await User.findOne({ email });
            if (!user) {
                return socket.emit('loginResponse', { success: false, code: 1, message: "계정없음" });
            }
            if (user.password === password) {
                socket.emit('loginResponse', { success: true, code: 0, message: "성공" });
            } else {
                socket.emit('loginResponse', { success: false, code: 2, message: "비번틀림" });
            }
        } catch (e) {
            console.error("[로그인 에러]", e);
            socket.emit('loginResponse', { success: false, code: 99, message: "에러" });
        }
    });

    // --- [1] 매칭 요청 처리 ---
    socket.on('requestMatchmaking', (data) => {
        const userEmail = data.email;
        if (matchmakingQueue.find(p => p.id === socket.id)) return;

        console.log(`[매칭] 대기열 합류: ${userEmail}`);
        matchmakingQueue.push({ id: socket.id, email: userEmail, socket: socket });

        if (matchmakingQueue.length >= 2) {
            const p1 = matchmakingQueue.shift();
            const p2 = matchmakingQueue.shift();
            const roomId = `room_${Date.now()}`;

            p1.socket.join(roomId);
            p2.socket.join(roomId);

            // 랜덤으로 '누가' 선공(흑돌)할지 결정
            const p1Starts = Math.random() < 0.5;

            gameRooms[roomId] = {
                black: p1Starts ? p1.id : p2.id, // 흑돌 ID 저장
                white: p1Starts ? p2.id : p1.id, // 백돌 ID 저장
                turn: p1Starts ? p1.id : p2.id   // 항상 흑돌(선공)부터 시작
            };

            // p1에게 전송
            p1.socket.emit('matchFound', {
                roomId: roomId,
                myPlayerNumber: 1,
                isMyTurn: p1Starts,        // p1Starts가 true면 p1이 흑돌 & 선공
                startingPlayer: p1Starts ? 1 : 2, // 누가 흑돌인지 알려줌
                opponentEmail: p2.email
            });

            // p2에게 전송
            p2.socket.emit('matchFound', {
                roomId: roomId,
                myPlayerNumber: 2,
                isMyTurn: !p1Starts,       // p1Starts가 false면 p2가 흑돌 & 선공
                startingPlayer: p1Starts ? 1 : 2,
                opponentEmail: p1.email
            });

            console.log(`[매칭 성사] ${p1.email} vs ${p2.email}`);
        }
    });

    // --- [2] 돌 놓기 처리 ---
    socket.on('placeStone', (data) => {
        const roomId = Array.from(socket.rooms).find(r => r.startsWith('room_'));
        if (!roomId || !gameRooms[roomId]) return;

        const room = gameRooms[roomId];
        if (socket.id !== room.turn) return;

        const playerNumber = (socket.id === room.black) ? 1 : 2;

        io.to(roomId).emit('stonePlaced', {
            row: data.row,
            col: data.col,
            player: playerNumber
        });

        room.turn = (socket.id === room.black) ? room.white : room.black;
    });

    // --- 접속 해제 처리 ---
    socket.on('disconnect', () => {
        matchmakingQueue = matchmakingQueue.filter(p => p.id !== socket.id);
        console.log(`[알림] 접속 해제: ${socket.id}`);
    });
});

server.listen(PORT, () => {
    console.log(`================================================`);
    console.log(`  서버 실행 중: http://localhost:${PORT}`);
    console.log(`================================================`);
});