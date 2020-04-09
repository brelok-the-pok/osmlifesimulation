using System;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary;
using OSMLSGlobalLibrary.Map;
using OSMLSGlobalLibrary.Modules;

namespace ScienceSubmarineAvoidingReefs
{
    public class TestModule : OSMLSModule
    {
        Polygon polygon;

        Reef reef;

        Submarine submarine;

        Random random;

        protected override void Initialize()
        {
            #region создание базовых объектов


            // Создание координат полигона.
            var polygonCoordinates = new Coordinate[] {
                    new Coordinate(-6500000, 4500000),
                    new Coordinate(-6500000, 2500000),
                    new Coordinate(-4500000, 2500000),
                    new Coordinate(-4500000, 4500000),
                    new Coordinate(-6500000, 4500000)
            };
            // Создание стандартного полигона по ранее созданным координатам.
            polygon = new Polygon(new LinearRing(polygonCoordinates));
            MapObjects.Add(polygon);



            reef = new Reef(-6300000, 4200000);
            MapObjects.Add(reef);

            reef = new Reef(-6000000, 3100000);
            MapObjects.Add(reef);

            reef = new Reef(-5400000, 4200000);
            MapObjects.Add(reef);

            reef = new Reef(-4800000, 3900000);
            MapObjects.Add(reef);

            reef = new Reef(-4700000, 3000000);
            MapObjects.Add(reef);

            #endregion

            #region 

            random = new Random(10);

            var submarineCoordinate = new Coordinate(-6300000, 4300000);

            int submarineSpeed = 10000;

            submarine = new Submarine(submarineCoordinate, submarineSpeed);

            MapObjects.Add(submarine);



            for (int i = 0; i < 10; i++)
            {
                int x = random.Next(-6500000, -4500000);
                int y = random.Next(2500000, 4500000);

                var obj = new IntrestingObj(new Coordinate(x, y));

                MapObjects.Add(obj);
            }

            #endregion
        }

        /// <summary>
        /// Вызывается постоянно, здесь можно реализовывать логику перемещений и всего остального, требующего времени.
        /// </summary>
        /// <param name="elapsedMilliseconds">TimeNow.ElapsedMilliseconds</param>
        public override void Update(long elapsedMilliseconds)
        {
            // Двигаем самолет.
            submarine.MoveToObj(MapObjects);

            if (MapObjects.GetAll<Point>().Count < 10)
            {
                int x = random.Next(-6500000, -4500000);
                int y = random.Next(2500000, 4500000);

                var obj = new IntrestingObj(new Coordinate(x, y));

                MapObjects.Add(obj);
            }
        }
    }

    #region
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 5,
                fill: new ol.style.Fill({
                    color: 'rgba(255, 0, 0, 1)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(255, 150, 150, 1)',
                    width: 1
                }),
            })
        });
        ")] 
    class Submarine : Point
    {
        public double Speed { get; }
        private int WaitingTime { get; set; }
        private int IsStuck { get; set; }
        private Reef StuckedReef { get; set; }
        public Submarine(Coordinate coordinate, double speed) : base(coordinate)
        {
            Speed = speed;

            WaitingTime = 0;

            IsStuck = 0;
        }

        public void MoveToObj(IInheritanceTreeCollection<Geometry> MapObjects)
        {
            IntrestingObj closestObj = FindClosestObj(MapObjects);

            if (IsStuck == 1)
            {
                Console.WriteLine("Я застрял, пытаюсь найти край рифа");

                IsStuck = 2;

                Coordinate reefDownLeftCoordinate = StuckedReef.Coordinates[1];
                if(FindRelativeCoords(reefDownLeftCoordinate).Y < 0)
                {
                    Y = StuckedReef.Coordinates[0].Y + 50000;
                }
                else
                {
                    Y = reefDownLeftCoordinate.Y - 50000;
                }

            }
            else if(IsStuck == 2)
            {
                Console.WriteLine("Край нашёл, теперь обхожу");
                IsStuck = 0;

                Coordinate reefDownLeftCoordinate = StuckedReef.Coordinates[1];
                if (FindRelativeCoords(reefDownLeftCoordinate).X < 0)
                {
                    X = StuckedReef.Coordinates[0].X - 22000;
                }
                else
                {
                    X = reefDownLeftCoordinate.X + 22000;
                }


                StuckedReef = null;
            }
            else
            {
                if (closestObj.Distance(this) <= Speed / 5)
                {
                    if (WaitingTime == 0)
                    {
                        X = closestObj.Coordinate.X;
                        Y = closestObj.Coordinate.Y;
                    }
                    else if (WaitingTime == 10)
                    {
                        MapObjects.Remove(closestObj);
                        WaitingTime = 0;
                    }
                    WaitingTime++;
                }

                Coordinate relativeCoord = FindRelativeCoords(closestObj.Coordinate);
                double curSpeedX = Math.Abs(relativeCoord.X) > Speed ? Speed : Speed / 5;
                double curSpeedY = Math.Abs(relativeCoord.Y) > Speed ? Speed : Speed / 5;

                double nextX = X + curSpeedX * Math.Sign(relativeCoord.X);
                double nextY = Y + curSpeedY * Math.Sign(relativeCoord.Y);

                var nextCoord = new Coordinate(nextX, nextY);

                Reef stuckedReef = CheckReefs(relativeCoord, MapObjects, nextCoord);

                if (stuckedReef == null)
                {
                    X += curSpeedX * Math.Sign(relativeCoord.X);
                    Y += curSpeedY * Math.Sign(relativeCoord.Y);
                }
                else
                {
                    IsStuck = 1;

                    StuckedReef = stuckedReef;
                }
            }

            
        }

        private IntrestingObj FindClosestObj(IInheritanceTreeCollection<Geometry> MapObjects)
        {
            var objects = MapObjects.GetAll<IntrestingObj>();

            IntrestingObj closest = objects[0];

            foreach (IntrestingObj obj in objects)
            {
                if (obj.Distance(this) < closest.Distance(this))
                {
                    closest = obj;
                }
            }
            return closest;
        }
        private Reef CheckReefs(Coordinate closestObjCoord, IInheritanceTreeCollection<Geometry> MapObjects, Coordinate nextCoord)
        {
            var reefs = MapObjects.GetAll<Reef>();
            
            var subLine = new LineSegment(Coordinate, nextCoord);

            foreach (Reef reef in reefs)
            {
                var reefLine = new LineSegment(reef.Coordinates[0], reef.Coordinates[1]);
                Coordinate coordinate = reefLine.Intersection(subLine);
                
                if (coordinate != null)
                {
                    Console.WriteLine($"rX1 = {reefLine.P0.X}; rY1 = {reefLine.P0.Y}");
                    Console.WriteLine($"rX2 = {reefLine.P1.X}; rY2 = {reefLine.P1.Y}");

                    Console.WriteLine($"X1 = {coordinate.X}; Y1 = {coordinate.Y}");
                    Console.WriteLine($"X2 = {nextCoord.X}; Y2 = {nextCoord.Y}");

                    Console.WriteLine($"interX = {coordinate.X}; interY = {coordinate.Y}");

                    return reef;
                }
            }
            return null;
        }

        private Coordinate FindRelativeCoords(Coordinate absoluteCoord)
        {
            return new Coordinate(absoluteCoord.X - X, absoluteCoord.Y - Y);//Относительные координаты ближайшей точки интереса
        }
    }

    #endregion


    #region
    /// <summary>
    /// Самолет, умеющий летать вверх-вправо с заданной скоростью.
    /// </summary>
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 4,
                fill: new ol.style.Fill({
                    color: 'rgba(0, 0, 255, 1)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 255, 255, 1)',
                    width: 1
                }),
            })
        });
        ")] // Переопределим стиль всех объектов данного класса, сделав самолет фиолетовым, используя атрибут CustomStyle.
    class IntrestingObj : Point
    {
        public IntrestingObj(Coordinate coordinate) : base(coordinate) { }
    }
    #endregion

    #region

    [CustomStyle(
        @"new ol.style.Style({
            fill: new ol.style.Fill(
            {
                    color: 'rgba(255, 255, 0, 1)'
            }),
            stroke: new ol.style.Stroke(
            {
                    color: 'rgba(255, 0, 0, 1)',
                    width: 1
            }),
        });
        ")]
    class Reef : Polygon
    {
        public Reef(int X, int Y)
            : base(new LinearRing(
                new Coordinate[] {
                    new Coordinate(X, Y),
                    new Coordinate(X, Y - 200000),
                    new Coordinate(X - 10000, Y - 200000),
                    new Coordinate(X - 10000, Y),
                    new Coordinate(X, Y)
            }))
        { }
    }
    #endregion
}
