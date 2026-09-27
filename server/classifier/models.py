from django.db import models


class SkinImage(models.Model):
    image = models.ImageField(upload_to="uploads/")
    prediction = models.CharField(max_length=255, blank=True, null=True)
    stats_result = models.JSONField(null=True, blank=True)  # Сюди пишуться результати статистики
    created_at = models.DateTimeField(auto_now_add=True)

    # NOTE: prediction and stats_result are computed in the view (views.py) before
    # the instance is saved. The previous save() override re-ran the model with
    # hardcoded dummy data and overwrote the real result, so it has been removed.

    def __str__(self):
        return f"Image {self.id} - {self.prediction}"
