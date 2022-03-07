using Microsoft.AspNetCore.Components.Web;
using BABYLON;
using EventHorizon.Blazor.Interop.Callbacks;
using babylon_wasm.Client;

namespace PongGame.Client.Pages
{

    public partial class Index : IDisposable
    {
        private Engine _engine;
        private DebugLayerScene _scene;
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {

            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await CreateSceneAsync();
            }
        }

        public void Dispose()
        {
            _engine?.dispose();
        }

        Game game;
        public async Task CreateSceneAsync()
        {

            var canvas = Canvas.GetElementById("game-window");
            var engine = new Engine(canvas, true);
            // extend layer scene untuk debug
            _scene = new DebugLayerScene(engine);

            HemisphericLight light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(1, 1, 0), _scene);
            light.intensity = 1;

            game = new Game(_scene);

            engine.runRenderLoop(new ActionCallback(() => Task.Run(() => _scene.render(true, false))));

            _engine = engine;
        }

        protected void HandleKeyDown(KeyboardEventArgs args)
        {
            Console.WriteLine(args.Key);
            switch (args.Key.ToLower())
            {
                case "w":
                    game?.MovePadUp();
                    break;
                case "s":
                    game?.MovePadDown();
                    break;

                default:
                    //do nothing
                    break;
            }
            if (args.ShiftKey && args.CtrlKey && args.AltKey && args.Key.ToLower() == "i")
            {
                if (_scene.debugLayer.isVisible())
                {
                    Console.WriteLine("Hello");
                    _scene.debugLayer.hide();
                }
                else
                {
                    _scene.debugLayer.show();
                }
            }
        }
    }

    public class Game
    {
        public enum Players
        {
            Human, Com
        }
        Scene scene;
        const decimal GameWidth = 20;
        const decimal GameHeight = 10;
        const decimal BallDiameter = 1;
        const decimal PadWidth = 3;
        const decimal PadDepth = 0.25m;
        const decimal PadToBorderDist = 1.5m;
        const decimal MovePadDist = 0.5m;
        const decimal BorderDepth = 0.25m;
        const decimal GapSize = 0.75m;

        //vars
        Mesh Ball;
        Mesh MyPad;
        Mesh ComPad;

        //score labels
        BABYLON.GUI.TextBlock PlayerScoreTxt;
        BABYLON.GUI.TextBlock ComScoreTxt;
        BABYLON.GUI.TextBlock StatusTxt;
        public decimal StartX { set; get; } = 0;
        public decimal StartY { set; get; } = 0;
        public Game(Scene scene, decimal X = 0, decimal Y = 0)
        {
            this.scene = scene;
            this.StartX = X;
            this.StartY = Y;
            InitScene();
        }

        public void MovePadUp(Players player = Players.Human)
        {
            if (player == Players.Human)
            {
                if (MyPad.position.z < GameHeight - (PadWidth / 2))
                    MyPad.position.z += MovePadDist;
            }
            else
            {
                if (ComPad.position.z < GameHeight - (PadWidth / 2))
                    ComPad.position.z += MovePadDist;
            }
        }

        public void MovePadDown(Players player = Players.Human)
        {
            if (player == Players.Human)
            {
                if (MyPad.position.z - (PadWidth / 2) > 0)
                    MyPad.position.z -= MovePadDist;
            }
            else
            {
                if (ComPad.position.z - (PadWidth / 2) > 0)
                    ComPad.position.z -= MovePadDist;
            }
        }

        void InitScene()
        {
            //border game
            var border_up = BABYLON.MeshBuilder.CreateBox("border-up", new { height = 1, width = GameWidth, depth = BorderDepth }, scene);
            var border_right = BABYLON.MeshBuilder.CreateBox("border-right", new { height = 1, width = GameHeight, depth = BorderDepth }, scene);
            var border_left = BABYLON.MeshBuilder.CreateBox("border-left", new { height = 1, width = GameHeight, depth = BorderDepth }, scene);
            var border_down = BABYLON.MeshBuilder.CreateBox("border-down", new { height = 1, width = GameWidth, depth = BorderDepth }, scene);
            border_up.position.x = StartX + (0.5m * GameWidth);
            border_up.position.z = StartY;

            border_down.position.x = StartX + (0.5m * GameWidth);
            border_down.position.z = StartY + GameHeight;

            border_left.position.x = StartX;
            border_left.position.z = StartY + (0.5m * GameHeight);
            border_left.rotate(BABYLON.Axis.Y, BABYLON.Tools.ToRadians(90));

            border_right.position.x = StartX + (GameWidth);
            border_right.position.z = StartY + (0.5m * GameHeight);
            border_right.rotate(BABYLON.Axis.Y, BABYLON.Tools.ToRadians(90));
            //ball
            Ball = BABYLON.MeshBuilder.CreateSphere("ball", new { diameter = BallDiameter }, scene);
            Ball.position = new Vector3(GameWidth / 2, 0, GameHeight / 2);
            var ballMat = new BABYLON.StandardMaterial("ballmat", scene);
            ballMat.diffuseColor = new BABYLON.Color3(0, 0.5m, 0.7m);
            Ball.material = ballMat;

            //pads
            MyPad = BABYLON.MeshBuilder.CreateBox("player-pad", new { height = 1, width = PadWidth, depth = PadDepth }, scene);
            ComPad = BABYLON.MeshBuilder.CreateBox("com-pad", new { height = 1, width = PadWidth, depth = PadDepth }, scene);

            MyPad.position.x = StartX + PadToBorderDist;
            MyPad.position.z = StartY + (0.5m * GameHeight);
            MyPad.rotate(BABYLON.Axis.Y, BABYLON.Tools.ToRadians(90));
            var playerpadmat = new BABYLON.StandardMaterial("playerpadmat", scene);
            playerpadmat.diffuseColor = new BABYLON.Color3(0.85m, 0, 0);
            MyPad.material = playerpadmat;

            ComPad.position.x = StartX + (GameWidth) - PadToBorderDist;
            ComPad.position.z = StartY + (0.5m * GameHeight);
            ComPad.rotate(BABYLON.Axis.Y, BABYLON.Tools.ToRadians(90));
            var compadmat = new BABYLON.StandardMaterial("compadmat", scene);
            compadmat.diffuseColor = new BABYLON.Color3(0, 0.85m, 0);
            ComPad.material = compadmat;

            //cam
            #region arc rotate cam
            var camera = new BABYLON.ArcRotateCamera("camera", (decimal)(-System.Math.PI / 2), (decimal)(System.Math.PI / 2.5), 30, new BABYLON.Vector3(StartX + (0.5m * GameWidth), 0, (0.5m * GameHeight)), scene);
            camera.upperBetaLimit = (decimal)(System.Math.PI / 2.2);
            camera.attachControl(true);
            #endregion

            //score
            var adt = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("UI", true, scene: scene);

            var panel = new BABYLON.GUI.StackPanel("stack1");
            panel.width = "220px";
            panel.top = "-25px";
            panel.horizontalAlignment = BABYLON.GUI.Control.HORIZONTAL_ALIGNMENT_RIGHT;
            panel.verticalAlignment = BABYLON.GUI.Control.VERTICAL_ALIGNMENT_BOTTOM;
            adt.addControl(panel);

            PlayerScoreTxt = new BABYLON.GUI.TextBlock("score1");
            PlayerScoreTxt.text = "Player: ";
            PlayerScoreTxt.height = "30px";
            PlayerScoreTxt.color = "white";
            panel.addControl(PlayerScoreTxt);

            ComScoreTxt = new BABYLON.GUI.TextBlock("score2");
            ComScoreTxt.text = "Com: ";
            ComScoreTxt.height = "30px";
            ComScoreTxt.color = "white";
            panel.addControl(ComScoreTxt);

            StatusTxt = new BABYLON.GUI.TextBlock("status");
            StatusTxt.text = "-";
            StatusTxt.height = "50px";
            StatusTxt.color = "red";
            panel.addControl(StatusTxt);

            var btn = BABYLON.GUI.Button.CreateSimpleButton("btnStart", "Start Game");
            btn.horizontalAlignment = BABYLON.GUI.Control.HORIZONTAL_ALIGNMENT_LEFT;
            btn.verticalAlignment = BABYLON.GUI.Control.VERTICAL_ALIGNMENT_BOTTOM;
            btn.textBlock.color = "white";
            btn.width = "200px";
            btn.height = "50px";
            btn.paddingLeft = "10px";
            btn.paddingBottom = "10px";
            btn.onPointerClickObservable.add((i, e) =>
            {
                StartGame();
                return Task.CompletedTask;
            });
            adt.addControl(btn);

        }
        void StartGame()
        {
            if (IsRunning && cts != null)
            {
                cts.Cancel();
                Thread.Sleep(200);
            }
            cts = new CancellationTokenSource();
            Ball.position = new Vector3(GameWidth / 2, 0, GameHeight / 2);
            gameThread = Task.Run(async () => Loop(cts.Token));
            gameThread.Start();
        }
        Task gameThread;
        public bool IsRunning { get; set; } = false;
        CancellationTokenSource cts;
        async Task Loop(CancellationToken GameToken)
        {

            decimal BallX = Ball.position.x, BallY = Ball.position.z, BallDX = 0.2m, BallDY = 0.4m;
            int ScoreHuman = 0, ScoreCom = 0;
            IsRunning = true;

            while (true)
            {
                if (GameToken.IsCancellationRequested)
                {

                    break;
                }

                BallX += BallDX;
                BallY += BallDY;

                if (BallX <= GapSize && BallDX < 0)
                {
                    //win
                    //play sound
                    ScoreCom++;
                    BallDX *= -1;
                }
                else if (BallX >= MyPad.position.x && BallX <= MyPad.position.x + GapSize && BallDX < 0 && BallY >= MyPad.position.z - (PadWidth / 2) && BallY <= MyPad.position.z + (PadWidth / 2))
                {
                    //pantul
                    BallDX *= -1;
                }

                if (BallX >= GameWidth - GapSize && BallDX > 0)
                {
                    //win
                    //play sound
                    ScoreHuman++;
                    BallDX *= -1;
                }
                else if (BallX + GapSize >= ComPad.position.x && BallX <= ComPad.position.x + PadDepth && BallDX > 0 && BallY >= ComPad.position.z - (PadWidth / 2) && BallY <= ComPad.position.z + (PadWidth / 2))
                {
                    //pantul
                    BallDX *= -1;
                }


                if (BallY <= GapSize || BallY >= GameHeight - GapSize)
                {
                    BallDY *= -1;

                }

                //move ball
                Ball.position.x = BallX;
                Ball.position.z = BallY;

                // Computer
                if (BallY > ComPad.position.z)
                    MovePadUp(Players.Com);
                else
                if (BallY < ComPad.position.z)
                    MovePadDown(Players.Com);

                // Score
                PlayerScoreTxt.text = $"Player: {ScoreHuman}";
                ComScoreTxt.text = $"Com: {ScoreCom}";

                if (ScoreHuman >= 5)
                {
                    StatusTxt.text = "You Win!";
                    break;
                }

                if (ScoreCom >= 5)
                {
                    StatusTxt.text = "You Lose!";
                    break;
                }

                await Task.Delay(50);
            }
            IsRunning = false;
        }
    }
}
