﻿using System.Numerics;
using TagCloudContainer.Core.Models;
using TagCloudContainer.Core.Interfaces;

namespace TagCloudContainer.Core.Utils;

public class TagCloudPlacer : ITagCloudPlacer
{
    private SortedList<float, Point> _nearestToTheCenterPoints = new SortedList<float, Point>();
    private List<Rectangle> _putRectangles = new List<Rectangle>();

    private readonly ITagCloudContainerConfig _tagCloudContainerConfig;
    private readonly ITagCloudPlacerConfig _tagCloudPlacerConfig;
    private readonly ISizeInvestigator _sizeInvestigator;
    private readonly ISelectedValues _selectedValues;

    public TagCloudPlacer(
        ITagCloudContainerConfig tagCloudContainerConfig,
        ISizeInvestigator sizeInvestigator,
        ITagCloudPlacerConfig tagCloudPlacerConfig,
        ISelectedValues selectedValues)
    {
        _tagCloudContainerConfig = tagCloudContainerConfig;
        _tagCloudPlacerConfig = tagCloudPlacerConfig;
        _selectedValues = selectedValues;
        
        _sizeInvestigator = sizeInvestigator;
    }

    public Result<Word> PlaceInCloud(Word word)
    {
        _nearestToTheCenterPoints = _tagCloudPlacerConfig.NearestToTheFieldCenterPoints;
        _putRectangles = _tagCloudPlacerConfig.PutRectangles;

        if (_nearestToTheCenterPoints.Count == 0)
            AddFreePoint(_tagCloudPlacerConfig.FieldCenter);

        var wordFontSize = new Font(_selectedValues.FontFamily,
            word.Weight * _tagCloudContainerConfig.StandartFontSize.Width);
        try
        {
            word.Size = TextRenderer
                .MeasureText(word.Value, wordFontSize);
            var nearestFreePoint = GetNearestInsertionPoint(word.Size);
            if (!nearestFreePoint.IsSuccess)
                return Result.Fail<Word>(nearestFreePoint.Error);
            var rectangle = new Rectangle(nearestFreePoint.Value, word.Size);

            _putRectangles.Add(rectangle);
            AddVerticesToFreePoints(rectangle);

            word.Position = new Point(rectangle.X, rectangle.Y);

            if (_sizeInvestigator.DidFit(word))
                return Result.Fail<Word>("Picture doesn't fit in form area");
        }
        finally
        {
            wordFontSize.Dispose();
        }

        return Result.Ok(word);
    }

    private Result<Point> GetNearestInsertionPoint(Size rectangleSize)
    {
        foreach (var nearestPoint in _nearestToTheCenterPoints.Values)
        {
            foreach (var rotateDirection in Enum.GetValues(typeof(RotateDirections)))
            {
                var insertionPoint = GetInsertionPoint(rectangleSize, nearestPoint, rotateDirection);
                var rectangle = new Rectangle(insertionPoint, rectangleSize);

                if (!DoesItIntersect(rectangle)) return insertionPoint.AsResult();
            }
        }

        return Result.Fail<Point>("Can't place this word");
    }

    private bool DoesItIntersect(Rectangle rectangle)
    {
        foreach (var putRectangle in _putRectangles)
            if (putRectangle.IntersectsWith(rectangle))
                return true;
        return false;
    }

    private Point GetInsertionPoint(Size rectangleSize, Point point, object rotateDirection)
    {
        var insertionPoint = new Point(point.X, point.Y);
        if (rotateDirection.Equals(RotateDirections.bottom))
            insertionPoint.X -= rectangleSize.Width;
        if (rotateDirection.Equals(RotateDirections.left))
            insertionPoint.Y -= rectangleSize.Height;

        return insertionPoint;
    }

    private void AddVerticesToFreePoints(Rectangle rectangle)
    {
        for (var i = 0; i <= rectangle.Width; i += rectangle.Width)
        for (var j = 0; j <= rectangle.Height; j += rectangle.Height)
            AddFreePoint(new Point(rectangle.X + i, rectangle.Y + j));
    }

    private void AddFreePoint(Point point)
    {
        var distanceFromCenter = CountDistanceFromCenter(point);
        if (!_nearestToTheCenterPoints.ContainsKey(distanceFromCenter))
            _nearestToTheCenterPoints.Add(distanceFromCenter, point);
    }

    private float CountDistanceFromCenter(Point point)
    {
        var distanceFromCenter = new Vector2(point.X - _tagCloudPlacerConfig.FieldCenter.X,
            point.Y - _tagCloudPlacerConfig.FieldCenter.Y);
        return distanceFromCenter.Length();
    }
}